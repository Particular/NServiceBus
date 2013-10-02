namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    ///     Helpers for assembly scanning operations
    /// </summary>
    public class AssemblyScanner
    {
        public AssemblyScanner()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        public AssemblyScanner(string baseDirectoryToScan)
        {
            AssembliesToInclude = new List<string>();
            AssembliesToSkip = new List<string>();
            mustReferenceAtLeastOneAssembly = new List<Assembly>();
            this.baseDirectoryToScan = baseDirectoryToScan;
            SetScanNestedDirectories();
            SetIncludeExesInScan();
        }

        public List<Assembly> MustReferenceAtLeastOneAssembly
        {
            get { return mustReferenceAtLeastOneAssembly; }
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
        ///     Traverses the specified base directory including all sub-directories, generating a list of assemblies that can be
        ///     scanned for handlers, a list of skipped files, and a list of errors that occurred while scanning.
        ///     Scanned files may be skipped when they're either not a .NET assembly, or if a reflection-only load of the .NET
        ///     assembly
        ///     reveals that it does not reference NServiceBus.
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
                    results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName,
                        "File was explicitly excluded from scanning."));
                    continue;
                }

                var compilationMode = Image.GetCompilationMode(assemblyFile.FullName);
                if (compilationMode == Image.CompilationMode.NativeOrInvalid)
                {
                    results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName, "File is not a .NET assembly."));
                    continue;
                }

                if (!Environment.Is64BitProcess && compilationMode == Image.CompilationMode.CLRx64)
                {
                    results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName,
                        "x64 .NET assembly can't be loaded by a 32Bit process."));
                    continue;
                }

                try
                {
                    if (MustReferenceAtLeastOneAssembly.Count > 0 && !AssemblyPassesReferencesTest(assemblyFile))
                    {
                        results.SkippedFiles.Add(new SkippedFile(assemblyFile.FullName,
                            "Assembly does not reference at least one of the must referenced assemblies."));
                        continue;
                    }

                    assembly = Assembly.LoadFrom(assemblyFile.FullName);
                }
                catch (BadImageFormatException)
                {
                    var errorMessage =
                        String.Format("Could not load '{0}'. Consider excluding that assembly from the scanning.",
                            assemblyFile.FullName);
                    results.Errors.Add(errorMessage);
                    continue;
                }

                try
                {
                    //will throw if assembly cannot be loaded
                    assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    var errorMessage = FormatReflectionTypeLoadException(assemblyFile.FullName, e);
                    results.Errors.Add(errorMessage);
                    continue;
                }

                results.Assemblies.Add(assembly);
            }

            return results;
        }

        public static string FormatReflectionTypeLoadException(string fileName, ReflectionTypeLoadException e)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Could not enumerate all types for '{0}'. Exception message: {1}.", fileName, e));

            if (!e.LoaderExceptions.Any())
            {
                return sb.ToString();
            }

            sb.AppendLine("Scanned type errors:");
            foreach (var ex in e.LoaderExceptions)
            {
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
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

        bool AssemblyPassesReferencesTest(FileSystemInfo assemblyFile)
        {
            var lightLoad = Assembly.ReflectionOnlyLoadFrom(assemblyFile.FullName);
            var referencedAssemblies = lightLoad.GetReferencedAssemblies();

            return MustReferenceAtLeastOneAssembly
                .Select(reference => reference.GetName().Name)
                .Any(name => referencedAssemblies.Any(a => a.Name == name));
        }

        /// <summary>
        ///     Determines whether the specified assembly name or file name can be included, given the set up include/exclude
        ///     patterns and default include/exclude patterns
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
            return !type.IsValueType &&
                   !(type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0);
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

        readonly string baseDirectoryToScan;
        readonly List<Assembly> mustReferenceAtLeastOneAssembly;
        public List<string> AssembliesToInclude;
        public List<string> AssembliesToSkip;

        public bool IncludeAppDomainAssemblies;
        public bool IncludeExesInScan = true;
        public bool ScanNestedDirectories = true;
    }
}