namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
#if NETCOREAPP
    using System.Runtime.Loader;
#endif
    using System.Text;
    using Logging;

    /// <summary>
    /// Helpers for assembly scanning operations.
    /// </summary>
    public class AssemblyScanner
    {
        /// <summary>
        /// Creates a new scanner that will scan the base directory of the current <see cref="AppDomain" />.
        /// </summary>
        public AssemblyScanner()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        /// <summary>
        /// Creates a scanner for the given directory.
        /// </summary>
        public AssemblyScanner(string baseDirectoryToScan)
        {
            this.baseDirectoryToScan = baseDirectoryToScan;
        }

        internal AssemblyScanner(Assembly assemblyToScan)
        {
            this.assemblyToScan = assemblyToScan;
        }

        /// <summary>
        /// Determines if the scanner should throw exceptions or not.
        /// </summary>
        public bool ThrowExceptions { get; set; } = true;

        /// <summary>
        /// Determines if the scanner should scan assemblies loaded in the <see cref="AppDomain.CurrentDomain" />.
        /// </summary>
        public bool ScanAppDomainAssemblies { get; set; } = true;

        /// <summary>
        /// Determines if the scanner should scan assemblies from the file system.
        /// </summary>
        public bool ScanFileSystemAssemblies { get; set; } = true;

        internal string CoreAssemblyName { get; set; } = NServicebusCoreAssemblyName;

        internal string AdditionalAssemblyScanningPath { get; set; }

        /// <summary>
        /// Traverses the specified base directory including all sub-directories, generating a list of assemblies that should be
        /// scanned for handlers, a list of skipped files, and a list of errors that occurred while scanning.
        /// </summary>
        public AssemblyScannerResults GetScannableAssemblies()
        {
            var results = new AssemblyScannerResults();
            var processed = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (assemblyToScan != null)
            {
                if (ScanAssembly(assemblyToScan, processed))
                {
                    AddTypesToResult(assemblyToScan, results);
                }

                return results;
            }

            // Always scan Core assembly
            var coreAssembly = typeof(AssemblyScanner).Assembly;
            if (ScanAssembly(coreAssembly, processed))
            {
                AddTypesToResult(coreAssembly, results);
            }

            if (ScanAppDomainAssemblies)
            {
                var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in appDomainAssemblies)
                {
                    if (ScanAssembly(assembly, processed))
                    {
                        AddTypesToResult(assembly, results);
                    }
                }
            }

            if (ScanFileSystemAssemblies)
            {
                var assemblies = new List<Assembly>();

                ScanAssembliesInDirectory(baseDirectoryToScan, assemblies, results);

                if (!string.IsNullOrWhiteSpace(AdditionalAssemblyScanningPath))
                {
                    ScanAssembliesInDirectory(AdditionalAssemblyScanningPath, assemblies, results);
                }

                var platformAssembliesString = (string)AppDomain.CurrentDomain.GetData("TRUSTED_PLATFORM_ASSEMBLIES");

                if (!string.IsNullOrEmpty(platformAssembliesString))
                {
                    var platformAssemblies = platformAssembliesString.Split(Path.PathSeparator);

                    foreach (var platformAssembly in platformAssemblies)
                    {
                        if (TryLoadScannableAssembly(platformAssembly, results, out var assembly))
                        {
                            assemblies.Add(assembly);
                        }
                    }
                }

                foreach (var assembly in assemblies)
                {
                    if (ScanAssembly(assembly, processed))
                    {
                        AddTypesToResult(assembly, results);
                    }
                }
            }

            results.RemoveDuplicates();
            return results;
        }

        void ScanAssembliesInDirectory(string directoryToScan, List<Assembly> assemblies, AssemblyScannerResults results)
        {
            foreach (var assemblyFile in ScanDirectoryForAssemblyFiles(directoryToScan, ScanNestedDirectories))
            {
                if (TryLoadScannableAssembly(assemblyFile.FullName, results, out var assembly))
                {
                    assemblies.Add(assembly);
                }
            }
        }

        bool TryLoadScannableAssembly(string assemblyPath, AssemblyScannerResults results, out Assembly assembly)
        {
            assembly = null;

            if (IsExcluded(Path.GetFileNameWithoutExtension(assemblyPath)))
            {
                var skippedFile = new SkippedFile(assemblyPath, "File was explicitly excluded from scanning.");
                results.SkippedFiles.Add(skippedFile);

                return false;
            }

            assemblyValidator.ValidateAssemblyFile(assemblyPath, out var shouldLoad, out var reason);

            if (!shouldLoad)
            {
                var skippedFile = new SkippedFile(assemblyPath, reason);
                results.SkippedFiles.Add(skippedFile);

                return false;
            }

            try
            {
#if NETCOREAPP
                var context = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
                assembly = context.LoadFromAssemblyPath(assemblyPath);
#else
                assembly = Assembly.LoadFrom(assemblyPath);
#endif
                return true;
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException)
            {
                results.ErrorsThrownDuringScanning = true;

                if (ThrowExceptions)
                {
                    var errorMessage = $"Could not load '{assemblyPath}'. Consider excluding that assembly from the scanning.";
                    throw new Exception(errorMessage, ex);
                }

                var skippedFile = new SkippedFile(assemblyPath, ex.Message);
                results.SkippedFiles.Add(skippedFile);

                return false;
            }
        }

        bool ScanAssembly(Assembly assembly, Dictionary<string, bool> processed)
        {
            if (assembly == null)
            {
                return false;
            }

            if (processed.TryGetValue(assembly.FullName, out var value))
            {
                return value;
            }

            processed[assembly.FullName] = false;

            if (assembly.GetName().Name == CoreAssemblyName)
            {
                return processed[assembly.FullName] = true;
            }

            if (ShouldScanDependencies(assembly))
            {
                foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
                {
                    var referencedAssembly = GetReferencedAssembly(referencedAssemblyName);
                    var referencesCore = ScanAssembly(referencedAssembly, processed);
                    if (referencesCore)
                    {
                        processed[assembly.FullName] = true;
                        break;
                    }
                }
            }

            return processed[assembly.FullName];
        }

        Assembly GetReferencedAssembly(AssemblyName assemblyName)
        {
            Assembly referencedAssembly = null;

            try
            {
                referencedAssembly = Assembly.Load(assemblyName);
            }
            catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException or FileLoadException) { }

            if (referencedAssembly == null)
            {
                referencedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
            }

            return referencedAssembly;
        }

        internal static string FormatReflectionTypeLoadException(string fileName, ReflectionTypeLoadException e)
        {
            var sb = new StringBuilder($"Could not enumerate all types for '{fileName}'.");

            if (!e.LoaderExceptions.Any())
            {
                sb.NewLine($"Exception message: {e}");
                return sb.ToString();
            }

            var nsbAssemblyName = typeof(AssemblyScanner).Assembly.GetName();
            var nsbPublicKeyToken = BitConverter.ToString(nsbAssemblyName.GetPublicKeyToken()).Replace("-", string.Empty).ToLowerInvariant();
            var displayBindingRedirects = false;
            var files = new List<string>();
            var sbFileLoadException = new StringBuilder();
            var sbGenericException = new StringBuilder();

            foreach (var ex in e.LoaderExceptions)
            {
                var loadException = ex as FileLoadException;

                if (loadException?.FileName != null)
                {
                    var assemblyName = new AssemblyName(loadException.FileName);
                    var assemblyPublicKeyToken = BitConverter.ToString(assemblyName.GetPublicKeyToken()).Replace("-", string.Empty).ToLowerInvariant();
                    if (nsbAssemblyName.Name == assemblyName.Name &&
                        nsbAssemblyName.CultureInfo.ToString() == assemblyName.CultureInfo.ToString() &&
                        nsbPublicKeyToken == assemblyPublicKeyToken)
                    {
                        displayBindingRedirects = true;
                        continue;
                    }

                    if (!files.Contains(loadException.FileName))
                    {
                        files.Add(loadException.FileName);
                        sbFileLoadException.NewLine(loadException.FileName);
                    }
                    continue;
                }

                sbGenericException.NewLine(ex.ToString());
            }

            if (sbGenericException.Length > 0)
            {
                sb.NewLine("Exceptions:");
                sb.Append(sbGenericException);
            }

            if (sbFileLoadException.Length > 0)
            {
                sb.AppendLine();
                sb.NewLine("It looks like you may be missing binding redirects in the config file for the following assemblies:");
                sb.Append(sbFileLoadException);
                sb.NewLine("For more information see http://msdn.microsoft.com/en-us/library/7wd6ex19(v=vs.100).aspx");
            }

            if (displayBindingRedirects)
            {
                sb.AppendLine();
                sb.NewLine("Try to add the following binding redirects to the config file:");

                const string bindingRedirects = @"<runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
        <dependentAssembly>
            <assemblyIdentity name=""NServiceBus.Core"" publicKeyToken=""9fc386479f8a226c"" culture=""neutral"" />
            <bindingRedirect oldVersion=""0.0.0.0-{0}"" newVersion=""{0}"" />
        </dependentAssembly>
    </assemblyBinding>
</runtime>";

                sb.NewLine(string.Format(bindingRedirects, nsbAssemblyName.Version.ToString(4)));
            }

            return sb.ToString();
        }

        static List<FileInfo> ScanDirectoryForAssemblyFiles(string directoryToScan, bool scanNestedDirectories)
        {
            var fileInfo = new List<FileInfo>();
            var baseDir = new DirectoryInfo(directoryToScan);
            var searchOption = scanNestedDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var searchPattern in FileSearchPatternsToUse)
            {
                foreach (var info in baseDir.GetFiles(searchPattern, searchOption))
                {
                    fileInfo.Add(info);
                }
            }
            return fileInfo;
        }

        bool IsExcluded(string assemblyNameOrFileName)
        {
            var isExplicitlyExcluded = AssembliesToSkip.Any(excluded => IsMatch(excluded, assemblyNameOrFileName));
            if (isExplicitlyExcluded)
            {
                return true;
            }

            var isExcludedByDefault = DefaultAssemblyExclusions.Any(exclusion => IsMatch(exclusion, assemblyNameOrFileName));
            if (isExcludedByDefault)
            {
                return true;
            }

            return false;
        }

        static bool IsMatch(string expression1, string expression2)
        {
            return DistillLowerAssemblyName(expression1) == DistillLowerAssemblyName(expression2);
        }

        bool IsAllowedType(Type type)
        {
            return type != null &&
                   !type.IsValueType &&
                   !IsCompilerGenerated(type) &&
                   !TypesToSkip.Contains(type);
        }

        static bool IsCompilerGenerated(Type type)
        {
            return type.GetCustomAttribute<CompilerGeneratedAttribute>(false) != null;
        }

        static string DistillLowerAssemblyName(string assemblyOrFileName)
        {
            var lowerAssemblyName = assemblyOrFileName.ToLowerInvariant();
            if (lowerAssemblyName.EndsWith(".dll") || lowerAssemblyName.EndsWith(".exe"))
            {
                lowerAssemblyName = lowerAssemblyName.Substring(0, lowerAssemblyName.Length - 4);
            }
            return lowerAssemblyName;
        }

        void AddTypesToResult(Assembly assembly, AssemblyScannerResults results)
        {
            try
            {
                //will throw if assembly cannot be loaded
                results.Types.AddRange(assembly.GetTypes().Where(IsAllowedType));
            }
            catch (ReflectionTypeLoadException e)
            {
                results.ErrorsThrownDuringScanning = true;

                var errorMessage = FormatReflectionTypeLoadException(assembly.FullName, e);
                if (ThrowExceptions)
                {
                    throw new Exception(errorMessage);
                }

                LogManager.GetLogger<AssemblyScanner>().Warn(errorMessage);
                results.Types.AddRange(e.Types.Where(IsAllowedType));
            }
            results.Assemblies.Add(assembly);
        }

        bool ShouldScanDependencies(Assembly assembly)
        {
            if (assembly.IsDynamic)
            {
                return false;
            }

            var assemblyName = assembly.GetName();

            if (assemblyName.Name == CoreAssemblyName)
            {
                return false;
            }

            if (AssemblyValidator.IsRuntimeAssembly(assemblyName.GetPublicKeyToken()))
            {
                return false;
            }

            if (IsExcluded(assemblyName.Name))
            {
                return false;
            }

            return true;
        }

        AssemblyValidator assemblyValidator = new AssemblyValidator();
        internal List<string> AssembliesToSkip = new List<string>();
        internal bool ScanNestedDirectories;
        internal List<Type> TypesToSkip = new List<Type>();
        Assembly assemblyToScan;
        string baseDirectoryToScan;
        const string NServicebusCoreAssemblyName = "NServiceBus.Core";

        static string[] FileSearchPatternsToUse =
        {
            "*.dll",
            "*.exe"
        };

        //TODO: delete when we make message scanning lazy #1617
        static string[] DefaultAssemblyExclusions =
        {
            // NSB Build-Dependencies
            "nunit",
            "nunit.framework",
            "nunit.applicationdomain",
            "nunit.engine",
            "nunit.engine.api",
            "nunit.engine.core",

            // NSB OSS Dependencies
            "nlog",
            "newtonsoft.json",
            "common.logging",
            "nhibernate",

            // Raven
            "raven.client",
            "raven.abstractions",

            // Azure host process, which is typically referenced for ease of deployment but should not be scanned
            "NServiceBus.Hosting.Azure.HostProcess.exe",

            // And other windows azure stuff
            "Microsoft.WindowsAzure"
        };
    }
}