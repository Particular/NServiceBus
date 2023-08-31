namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
#if NET
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

        internal string CoreAssemblyName { get; set; } = NServiceBusCoreAssemblyName;

        internal IReadOnlyCollection<string> AssembliesToSkip
        {
            set => assembliesToSkip = new HashSet<string>(value.Select(Path.GetFileNameWithoutExtension), StringComparer.OrdinalIgnoreCase);
        }

        internal IReadOnlyCollection<Type> TypesToSkip
        {
            set => typesToSkip = new HashSet<Type>(value);
        }

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
#if NET
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

            if (e.LoaderExceptions.Length == 0)
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
                fileInfo.AddRange(baseDir.GetFiles(searchPattern, searchOption));
            }
            return fileInfo;
        }

        bool IsExcluded(string assemblyNameOrFileNameWithoutExtension) =>
            assembliesToSkip.Contains(assemblyNameOrFileNameWithoutExtension) ||
            DefaultAssemblyExclusions.Contains(assemblyNameOrFileNameWithoutExtension);

        // The parameter and return types of this method are deliberately using the most concrete types
        // to avoid unnecessary allocations
        List<Type> FilterAllowedTypes(Type[] types)
        {
            // assume the majority of types will be allowed to preallocate the list
            var allowedTypes = new List<Type>(types.Length);
            foreach (var typeToAdd in types)
            {
                if (IsAllowedType(typeToAdd))
                {
                    allowedTypes.Add(typeToAdd);
                }
            }
            return allowedTypes;
        }

        bool IsAllowedType(Type type) =>
            type is { IsValueType: false } &&
            Attribute.GetCustomAttribute(type, typeof(CompilerGeneratedAttribute), false) == null &&
            !typesToSkip.Contains(type);

        void AddTypesToResult(Assembly assembly, AssemblyScannerResults results)
        {
            try
            {
                var types = assembly.GetTypes();
                results.Types.AddRange(FilterAllowedTypes(types));
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
                results.Types.AddRange(FilterAllowedTypes(e.Types));
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
        internal bool ScanNestedDirectories;
        Assembly assemblyToScan;
        string baseDirectoryToScan;
        HashSet<Type> typesToSkip = new();
        HashSet<string> assembliesToSkip = new(StringComparer.OrdinalIgnoreCase);
        const string NServiceBusCoreAssemblyName = "NServiceBus.Core";

        static string[] FileSearchPatternsToUse =
        {
            "*.dll",
            "*.exe"
        };

        static HashSet<string> DefaultAssemblyExclusions = new(StringComparer.OrdinalIgnoreCase)
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