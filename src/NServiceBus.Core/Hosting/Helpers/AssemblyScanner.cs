namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
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
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class AssemblyScanner
    {
        public List<string> AssembliesToSkip;
        public List<string> AssembliesToInclude;
        string baseDirectoryToScan;
        public bool IncludeAppDomainAssemblies;
        
        // default (should be changed in 5.0)
        public bool ScanNestedDirectories = true;
        
        // default (should be changed in 5.0)
        public bool IncludeExesInScan = true;

        public AssemblyScanner()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public AssemblyScanner(string baseDirectoryToScan)
        {
            AssembliesToInclude = new List<string>();
            AssembliesToSkip = new List<string>();
            this.baseDirectoryToScan = baseDirectoryToScan;
            SetScanNestedDirectories();
            SetIncludeExesInScan();
        }

        [ObsoleteEx(
            RemoveInVersion = "5.0",
            Message = @"Defaults to scan sub-directories. In the future, 'ScanNestedDirectories' will be opt-in.")]
        void SetScanNestedDirectories()
        {
            bool scanNestedDirectories;
            var appSetting = ConfigurationManager.AppSettings["NServiceBus/AssemblyScanning/ScanNestedDirectories"];
            if (bool.TryParse(appSetting, out scanNestedDirectories))
            {
                ScanNestedDirectories = scanNestedDirectories;
            }
        }

        [ObsoleteEx(
            RemoveInVersion = "5.0",
            Message = @"Defaults to pick up .exe files. In the future, 'IncludeExesInScan' will be opt-in.")]
        void SetIncludeExesInScan()
        {
            bool includeExesInScan;
            var appSetting = ConfigurationManager.AppSettings["NServiceBus/AssemblyScanning/IncludeExesInScan"];
            if (bool.TryParse(appSetting, out includeExesInScan))
            {
                IncludeExesInScan = includeExesInScan;
            }
        }

        /// <summary>
        /// Traverses the specified base directory including all sub-directories, generating a list of assemblies that can be
        /// scanned for handlers, a list of skipped files, and a list of errors that occurred while scanning.
        /// Scanned files may be skipped when they're either not a .NET assembly, or if a reflection-only load of the .NET assembly
        /// reveals that it does not reference NServiceBus.
        /// </summary>
        public AssemblyScannerResults GetScannableAssemblies()
        {
            var results = new AssemblyScannerResults();

            if (IncludeAppDomainAssemblies)
            {
                var matchingAssembliesFromAppDomain = AppDomain.CurrentDomain
                                                           .GetAssemblies()
                                                           .Where(assembly => IsIncluded(assembly.GetName().Name));

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
                    var errorMessage = string.Format("Could not load {0}. Consider using 'Configure.With(AllAssemblies.Except(\"{1}\"))' to tell NServiceBus not to load this file.", assemblyFile.FullName, assemblyFile.Name);
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
            var searchOption = ScanNestedDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return GetFileSearchPatternsToUse()
                .SelectMany(extension => baseDir.GetFiles(extension, searchOption))
                .ToList();
        }

        IEnumerable<string> GetFileSearchPatternsToUse()
        {
            yield return "*.dll";

            if (IncludeExesInScan)
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
            var isExplicitlyExcluded = AssembliesToSkip.Any(excluded => IsMatch(excluded, assemblyNameOrFileName));

            if (isExplicitlyExcluded)
            {
                return false;
            }

            var noAssembliesWereExplicitlyIncluded = !AssembliesToInclude.Any();
            var isExplicitlyIncluded = AssembliesToInclude.Any(included => IsMatch(included, assemblyNameOrFileName));

            return noAssembliesWereExplicitlyIncluded || isExplicitlyIncluded;
        }

        static bool IsMatch(string expression, string scopedNameOrFileName)
        {
            if (DistillLowerAssemblyName(scopedNameOrFileName).StartsWith(expression.ToLower()))
            {
                return true;
            }

            if (DistillLowerAssemblyName(expression).TrimEnd('.') == DistillLowerAssemblyName(scopedNameOrFileName))
            {
                return true;
            }

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