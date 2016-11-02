namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Logging;

    /// <summary>
    /// Helpers for assembly scanning operations.
    /// </summary>
    public partial class AssemblyScanner
    {
        /// <summary>
        /// Creates a new scanner that will scan the base directory of the current appdomain.
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
            ThrowExceptions = true;
            this.baseDirectoryToScan = baseDirectoryToScan;
            CoreAssemblyName = NServicebusCoreAssemblyName;
        }

        internal AssemblyScanner(Assembly assemblyToScan)
        {
            this.assemblyToScan = assemblyToScan;
            ThrowExceptions = true;
            CoreAssemblyName = NServicebusCoreAssemblyName;
        }

        /// <summary>
        /// Determines if the scanner should throw exceptions or not.
        /// </summary>
        public bool ThrowExceptions { get; set; }

        internal string CoreAssemblyName { get; set; }

        /// <summary>
        /// Traverses the specified base directory including all sub-directories, generating a list of assemblies that can be
        /// scanned for handlers, a list of skipped files, and a list of errors that occurred while scanning.
        /// Scanned files may be skipped when they're either not a .NET assembly, or if a reflection-only load of the .NET
        /// assembly reveals that it does not reference NServiceBus.
        /// </summary>
        public AssemblyScannerResults GetScannableAssemblies()
        {
            var results = new AssemblyScannerResults();
            var processed = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (assemblyToScan != null)
            {
                var assemblyPath = AssemblyPath(assemblyToScan);
                ScanAssembly(assemblyPath, results, processed);
                return results;
            }

            foreach (var assemblyFile in ScanDirectoryForAssemblyFiles(baseDirectoryToScan, ScanNestedDirectories))
            {
                ScanAssembly(assemblyFile.FullName, results, processed);
            }

            // This extra step is to ensure unobtrusive message types are included in the Types list.
            var list = GetHandlerMessageTypes(results.Types);
            results.Types.AddRange(list);

            results.RemoveDuplicates();

            return results;
        }

        static List<Type> GetHandlerMessageTypes(List<Type> list)
        {
            var foundMessageTypes = new List<Type>();
            foreach (var type in list)
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                foreach (var @interface in type.GetInterfaces())
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == IHandleMessagesType)
                    {
                        var messageType = @interface.GetGenericArguments()[0];
                        foundMessageTypes.Add(messageType);
                    }
                }
            }
            return foundMessageTypes;
        }

        static string AssemblyPath(Assembly assembly)
        {
            var uri = new UriBuilder(assembly.CodeBase);
            return Uri.UnescapeDataString(uri.Path).Replace('/', '\\');
        }

        void ScanAssembly(string assemblyPath, AssemblyScannerResults results, Dictionary<string, bool> processed)
        {
            Assembly assembly;

            if (!IsIncluded(Path.GetFileNameWithoutExtension(assemblyPath)))
            {
                var skippedFile = new SkippedFile(assemblyPath, "File was explicitly excluded from scanning.");
                results.SkippedFiles.Add(skippedFile);
                return;
            }

            var compilationMode = Image.GetCompilationMode(assemblyPath);
            if (compilationMode == Image.CompilationMode.NativeOrInvalid)
            {
                var skippedFile = new SkippedFile(assemblyPath, "File is not a .NET assembly.");
                results.SkippedFiles.Add(skippedFile);
                return;
            }

            if (!Environment.Is64BitProcess && compilationMode == Image.CompilationMode.CLRx64)
            {
                var skippedFile = new SkippedFile(assemblyPath, "x64 .NET assembly can't be loaded by a 32Bit process.");
                results.SkippedFiles.Add(skippedFile);
                return;
            }

            try
            {
                if (!ReferencesNServiceBus(assemblyPath, processed, CoreAssemblyName))
                {
                    var skippedFile = new SkippedFile(assemblyPath, "Assembly does not reference at least one of the must referenced assemblies.");
                    results.SkippedFiles.Add(skippedFile);
                    return;
                }

                assembly = Assembly.LoadFrom(assemblyPath);

                if (results.Assemblies.Contains(assembly))
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException)
            {
                assembly = null;
                results.ErrorsThrownDuringScanning = true;

                if (ThrowExceptions)
                {
                    var errorMessage = $"Could not load '{assemblyPath}'. Consider excluding that assembly from the scanning.";
                    throw new Exception(errorMessage, ex);
                }
            }

            if (assembly == null)
            {
                return;
            }

            try
            {
                //will throw if assembly cannot be loaded
                results.Types.AddRange(assembly.GetTypes().Where(IsAllowedType));
            }
            catch (ReflectionTypeLoadException e)
            {
                results.ErrorsThrownDuringScanning = true;

                var errorMessage = FormatReflectionTypeLoadException(assemblyPath, e);
                if (ThrowExceptions)
                {
                    throw new Exception(errorMessage);
                }

                LogManager.GetLogger<AssemblyScanner>().Warn(errorMessage);
                results.Types.AddRange(e.Types.Where(IsAllowedType));
            }

            results.Assemblies.Add(assembly);
        }

        internal static bool IsRuntimeAssembly(string assemblyPath)
        {
            var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
            return IsRuntimeAssembly(assemblyName);
        }

        internal static bool IsRuntimeAssembly(AssemblyName assemblyName)
        {
            var publicKeyToken = assemblyName.GetPublicKeyToken();
            var lowerInvariant = BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToLowerInvariant();
            //System
            if (lowerInvariant == "b77a5c561934e089")
            {
                return true;
            }

            //System.Core +  mscorelib
            if (lowerInvariant == "7cec85d7bea7798e")
            {
                return true;
            }

            //Web
            if (lowerInvariant == "b03f5f7f11d50a3a")
            {
                return true;
            }

            //patterns and practices
            if (lowerInvariant == "31bf3856ad364e35")
            {
                return true;
            }

            return false;
        }

        static string FormatReflectionTypeLoadException(string fileName, ReflectionTypeLoadException e)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Could not enumerate all types for '{fileName}'.");

            if (!e.LoaderExceptions.Any())
            {
                sb.AppendLine($"Exception message: {e}");
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
                        sbFileLoadException.AppendLine(loadException.FileName);
                    }
                    continue;
                }

                sbGenericException.AppendLine(ex.ToString());
            }

            if (sbGenericException.ToString().Length > 0)
            {
                sb.AppendLine("Exceptions:");
                sb.AppendLine(sbGenericException.ToString());
            }

            if (sbFileLoadException.ToString().Length > 0)
            {
                sb.AppendLine("It looks like you may be missing binding redirects in the config file for the following assemblies:");
                sb.Append(sbFileLoadException);
                sb.AppendLine("For more information see http://msdn.microsoft.com/en-us/library/7wd6ex19(v=vs.100).aspx");
            }

            if (displayBindingRedirects)
            {
                sb.AppendLine();
                sb.AppendLine("Try to add the following binding redirects to the config file:");

                const string bindingRedirects = @"<runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
        <dependentAssembly>
            <assemblyIdentity name=""NServiceBus.Core"" publicKeyToken=""9fc386479f8a226c"" culture=""neutral"" />
            <bindingRedirect oldVersion=""0.0.0.0-{0}"" newVersion=""{0}"" />
        </dependentAssembly>
    </assemblyBinding>
</runtime>";

                sb.AppendFormat(bindingRedirects, nsbAssemblyName.Version.ToString(4));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        static List<FileInfo> ScanDirectoryForAssemblyFiles(string directoryToScan, bool scanNestedDirectories)
        {
            var fileInfo = new List<FileInfo>();
            var baseDir = new DirectoryInfo(directoryToScan);
            var searchOption = scanNestedDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var searchPatern in FileSearchPatternsToUse)
            {
                foreach (var info in baseDir.GetFiles(searchPatern, searchOption))
                {
                    fileInfo.Add(info);
                }
            }
            return fileInfo;
        }

        internal static bool ReferencesNServiceBus(string assemblyPath, Dictionary<string, bool> processed, string coreAssemblyName = NServicebusCoreAssemblyName)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            if (assembly.GetName().Name == coreAssemblyName)
            {
                return true;
            }
            return ReferencesNServiceBus(assembly, coreAssemblyName, processed);
        }

        static bool ReferencesNServiceBus(Assembly assembly, string coreAssemblyName, Dictionary<string, bool> processed)
        {
            var name = assembly.GetName().FullName;
            if (processed.ContainsKey(name))
            {
                return processed[name];
            }

            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                bool referencesCore;
                var assemblyNameFullName = assemblyName.FullName;
                if (processed.TryGetValue(assemblyNameFullName, out referencesCore))
                {
                    if (referencesCore)
                    {
                        processed[name] = true;
                        break;
                    }

                    continue;
                }

                if (assemblyName.Name == coreAssemblyName)
                {
                    processed[assemblyNameFullName] = true;
                    return true;
                }

                if (IsRuntimeAssembly(assemblyName))
                {
                    processed[assemblyNameFullName] = false;
                    continue;
                }
                //We need to do a ApplyPolicy, and use the result, so as to respect the current binding redirects
                var afterPolicyName = AppDomain.CurrentDomain.ApplyPolicy(assemblyNameFullName);
                Assembly refAssembly;
                try
                {
                    refAssembly = Assembly.ReflectionOnlyLoad(afterPolicyName);
                }
                catch (FileNotFoundException)
                {
                    processed[assemblyNameFullName] = false;
                    continue;
                }
                catch (FileLoadException)
                {
                    processed[assemblyNameFullName] = false;
                    continue;
                }
                if (ReferencesNServiceBus(refAssembly, coreAssemblyName, processed))
                {
                    processed[assemblyNameFullName] = true;
                    return true;
                }
            }
            return processed.ContainsKey(name) && processed[name];
        }

        bool IsIncluded(string assemblyNameOrFileName)
        {
            var isExplicitlyExcluded = AssembliesToSkip.Any(excluded => IsMatch(excluded, assemblyNameOrFileName));
            if (isExplicitlyExcluded)
            {
                return false;
            }

            var isExcludedByDefault = DefaultAssemblyExclusions.Any(exclusion => IsMatch(exclusion, assemblyNameOrFileName));
            if (isExcludedByDefault)
            {
                return false;
            }

            return true;
        }

        static bool IsMatch(string expression1, string expression2)
        {
            return DistillLowerAssemblyName(expression1) == DistillLowerAssemblyName(expression2);
        }

        bool IsAllowedType(Type type)
        {
            return type != null &&
                   !type.IsValueType &&
                   !(type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0) &&
                   !TypesToSkip.Contains(type);
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

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
    }
}
