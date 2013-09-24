namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    /// Helpers for assembly scanning operations
    /// </summary>
    public class AssemblyScanner
    {
        readonly List<string> assembliesToSkip = new List<string>();
        readonly List<string> assembliesToInclude = new List<string>();
        readonly string baseDirectoryToScan;

        bool includeAppDomainAssemblies;
        bool recurse;
        bool includeExeInScan;

        public AssemblyScanner()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public AssemblyScanner(string baseDirectoryToScan)
        {
            this.baseDirectoryToScan = baseDirectoryToScan;

            SetCompatibilityMode();
        }

        [ObsoleteEx(RemoveInVersion = "5.0", Message = "AssemblyScanner now defaults to work in 'compatibility mode', i.e. it includes subdirs in the scan and picks up .exe files as well. In the future, 'compatibility mode' should be opt-in instead of opt-out")]
        void SetCompatibilityMode()
        {
            bool compatibilityMode;
            if (!bool.TryParse(ConfigurationManager.AppSettings["NServiceBus/AssemblyScanning/CompatibilityMode"],
                               out compatibilityMode))
            {
                // default (should be changed in 5.0)
                ScanExeFiles(true);
                ScanSubdirectories(true);
                return;
            }
        
            // adjust behavior depending on mode
            if (compatibilityMode)
            {
                ScanExeFiles(true);
                ScanSubdirectories(true);
            }
            else
            {
                ScanExeFiles(false);
                ScanSubdirectories(false);
            }
        }

        public AssemblyScanner IncludeAppDomainAssemblies()
        {
            includeAppDomainAssemblies = true;
            return this;
        }

        public AssemblyScanner ScanSubdirectories(bool shouldRecurse)
        {
            recurse = shouldRecurse;
            return this;
        }

        public AssemblyScanner ScanExeFiles(bool shouldScanForExeFiles)
        {
            includeExeInScan = shouldScanForExeFiles;
            return this;
        }

        public AssemblyScanner IncludeAssemblies(IEnumerable<string> assembliesToAddToListOfIncludedAssemblies)
        {
            if (assembliesToAddToListOfIncludedAssemblies != null)
            {
                assembliesToInclude.AddRange(assembliesToAddToListOfIncludedAssemblies);
            }
            return this;
        }

        public AssemblyScanner ExcludeAssemblies(IEnumerable<string> assembliesToAddToListOfSkippedAssemblies)
        {
            if (assembliesToAddToListOfSkippedAssemblies != null)
            {
                assembliesToSkip.AddRange(assembliesToAddToListOfSkippedAssemblies);
            }
            return this;
        }

        /// <summary>
        /// Traverses the specified base directory including all subdirectories, generating a list of assemblies that can be
        /// scanned for handlers, a list of skipped files, and a list of errors that occurred while scanning.
        /// Scanned files may be skipped when they're either not a .NET assembly, or if a reflection-only load of the .NET assembly
        /// reveals that it does not reference NServiceBus.
        /// </summary>
        [DebuggerNonUserCode]
        public AssemblyScannerResults GetScannableAssemblies()
        {
            var results = new AssemblyScannerResults();

            if (includeAppDomainAssemblies)
            {
                var matchingAssembliesFromAppDomain = AppDomain.CurrentDomain
                                                           .GetAssemblies()
                                                           .Where(assembly => IsIncluded(assembly.GetName().Name))
                                                           .ToArray();

                results.Assemblies.AddRange(matchingAssembliesFromAppDomain);
            }

            var assemblyFiles = ScanDirectoryForAssemblyFiles();

            foreach (var assemblyFile in assemblyFiles)
            {
                Assembly assembly;

                if (!IsIncluded(assemblyFile.Name))
                {
                    results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName, "File was explicitly excluded from scanning"));
                    continue;
                }

                var compilationMode = Image.GetCompilationMode(assemblyFile.FullName);
                if (compilationMode == Image.CompilationMode.NativeOrInvalid)
                {
                    results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName, "File is not a .NET assembly"));
                    continue;
                }

                if (!Environment.Is64BitProcess && compilationMode == Image.CompilationMode.CLRx64)
                {
                    results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName, "x64 .NET assembly can't be loaded by a 32Bit process"));
                    continue;
                }

                try
                {
                    if (!AssemblyReferencesNServiceBus(assemblyFile))
                    {
                        results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName, "Assembly does not reference NServiceBus and thus cannot contain any handlers"));
                        continue;
                    }

                    assembly = Assembly.LoadFrom(assemblyFile.FullName);
                }
                catch (BadImageFormatException badImageFormatException)
                {
                    var errorMessage = string.Format("Could not load {0}. Consider using 'Configure.With(AllAssemblies.Except(\"{1}\"))'" +
                                                     " to tell NServiceBus not to load this file.", assemblyFile.FullName, assemblyFile.Name);
                    var error = new ErrorWhileScanningAssemblies(badImageFormatException, errorMessage);
                    results.Errors.Add(error);
                    continue;
                }

                try
                {
                    //will throw if assembly cannot be loaded
                    assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("Could not scan assembly: {0}. Exception message {1}.", assemblyFile.FullName, e);
                    if (e.LoaderExceptions.Any())
                    {
                        sb.Append(Environment.NewLine + "Scanned type errors: ");
                        foreach (var ex in e.LoaderExceptions)
                        {
                            sb.Append(Environment.NewLine + ex.Message);
                        }
                    }
                    var error = new ErrorWhileScanningAssemblies(e, sb.ToString());
                    results.Errors.Add(error);
                    continue;
                }

                results.Assemblies.Add(assembly);
            }

            return results;
        }

        IEnumerable<FileInfo> ScanDirectoryForAssemblyFiles()
        {
            var baseDir = new DirectoryInfo(baseDirectoryToScan);
            var searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var assemblyFiles = GetFileSearchPatternsToUse()
                .SelectMany(extension => baseDir.GetFiles(extension, searchOption))
                .ToList();

            return assemblyFiles;
        }

        IEnumerable<string> GetFileSearchPatternsToUse()
        {
            yield return "*.dll";

            if (includeExeInScan)
            {
                yield return "*.exe";
            }
        }

        bool AssemblyReferencesNServiceBus(FileSystemInfo assemblyFile)
        {
            var lightLoad = Assembly.ReflectionOnlyLoadFrom(assemblyFile.FullName);
            var referencedAssemblies = lightLoad.GetReferencedAssemblies();

            var nameOfAssemblyDefiningHandlersInterface =
                typeof(IHandleMessages<>).Assembly.GetName().Name;

            return referencedAssemblies
                .Any(a => a.Name == nameOfAssemblyDefiningHandlersInterface);
        }

        /// <summary>
        /// Determines whether the specified assembly name or file name can be included, given the set up include/exclude
        /// patterns and default include/exclude patterns
        /// </summary>
        bool IsIncluded(string assemblyNameOrFileName)
        {
            var isExplicitlyExcluded = assembliesToSkip.Any(excluded => IsMatch(excluded, assemblyNameOrFileName));

            if (isExplicitlyExcluded)
                return false;

            var noAssembliesWereExplicitlyIncluded = !assembliesToInclude.Any();
            var isExplicitlyIncluded = assembliesToInclude.Any(included => IsMatch(included, assemblyNameOrFileName));

            return noAssembliesWereExplicitlyIncluded || isExplicitlyIncluded;
        }

        static bool IsMatch(string expression, string scopedNameOrFileName)
        {
            if (DistillLowerAssemblyName(scopedNameOrFileName).StartsWith(expression.ToLower()))
                return true;

            if (DistillLowerAssemblyName(expression).TrimEnd('.') == DistillLowerAssemblyName(scopedNameOrFileName))
                return true;

            return false;
        }

        public static bool IsAllowedType(Type type)
        {
            return !type.IsValueType && !(type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0);
        }

        static string DistillLowerAssemblyName(string assemblyOrFileName)
        {
            var lowerAssemblyName = assemblyOrFileName.ToLowerInvariant();
            if (lowerAssemblyName.EndsWith(".dll"))
            {
                lowerAssemblyName = lowerAssemblyName.Substring(0, lowerAssemblyName.Length - 4);
            }
            return lowerAssemblyName;
        }
    }
}