namespace NServiceBus.Hosting.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;

    /// <summary>
    ///   Helpers for assembly scanning operations
    /// </summary>
    public class AssemblyScanner
    {
        /// <summary>
        /// Creates a new scanner that will scan the base directory of the current appdomain
        /// </summary>
        public AssemblyScanner()
            : this(AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        /// <summary>
        /// Creates a scanner for the given directory
        /// </summary>
        /// <param name="baseDirectoryToScan"></param>
        public AssemblyScanner(string baseDirectoryToScan)
        {
            ThrowExceptions = true;
            AssembliesToInclude = new List<string>();
            AssembliesToSkip = new List<string>();
            MustReferenceAtLeastOneAssembly = new List<Assembly>();
            this.baseDirectoryToScan = baseDirectoryToScan;
        }

        /// <summary>
        /// Tells the scanner to only include assemblies that reference one of the given assemblies
        /// </summary>
        public List<Assembly> MustReferenceAtLeastOneAssembly { get; private set; }

        

        /// <summary>
        ///     Traverses the specified base directory including all sub-directories, generating a list of assemblies that can be
        ///     scanned for handlers, a list of skipped files, and a list of errors that occurred while scanning.
        ///     Scanned files may be skipped when they're either not a .NET assembly, or if a reflection-only load of the .NET
        ///     assembly reveals that it does not reference NServiceBus.
        /// </summary>
        public AssemblyScannerResults GetScannableAssemblies()
        {
            var results = new AssemblyScannerResults();

            // Include AppDomain loaded assemblies all the time
            results.Assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(a=> !IsInDefaultAssemblyExclusions(a.GetName().Name)));

            foreach (var assemblyFile in ScanDirectoryForAssemblyFiles())
            {
                ScanAssembly(assemblyFile.FullName, results);
            }

            return results;
        }

        /// <summary>
        /// Determines if the scanner should throw exceptions or not
        /// </summary>
        public bool ThrowExceptions { get; set; }

        void ScanAssembly(string assemblyPath, AssemblyScannerResults results)
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
                //TODO: re-enable when we make message scanning lazy #1617
                //if (!AssemblyPassesReferencesTest(assemblyPath))
                //{
                //    var skippedFile = new SkippedFile(assemblyPath, "Assembly does not reference at least one of the must referenced assemblies.");
                //    results.SkippedFiles.Add(skippedFile);
                //    return;
                //}
                if (IsRuntimeAssembly(assemblyPath))
                {
                    var skippedFile = new SkippedFile(assemblyPath, "Assembly .net runtime assembly.");
                    results.SkippedFiles.Add(skippedFile);
                    return;
                }

                assembly = Assembly.LoadFrom(assemblyPath);

                if (results.Assemblies.Contains(assembly))
                {
                    return;
                }
            }
            catch (BadImageFormatException ex)
            {
                assembly = null;
                results.ErrorsThrownDuringScanning = true;

                if (ThrowExceptions)
                {
                    var errorMessage = String.Format("Could not load '{0}'. Consider excluding that assembly from the scanning.", assemblyPath);
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
                assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                results.ErrorsThrownDuringScanning = true;

                if (ThrowExceptions)
                {
                    var errorMessage = FormatReflectionTypeLoadException(assemblyPath, e);
                    throw new Exception(errorMessage);
                }

                return;
            }

            results.Assemblies.Add(assembly);
        }


        internal static bool IsRuntimeAssembly(string assemblyPath)
        {
            var publicKeyToken = AssemblyName.GetAssemblyName(assemblyPath).GetPublicKeyToken();
            var lowerInvariant = BitConverter.ToString(publicKeyToken).Replace("-", "").ToLowerInvariant();
            //System
            if (lowerInvariant == "b77a5c561934e089")
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

        internal static string FormatReflectionTypeLoadException(string fileName, ReflectionTypeLoadException e)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Could not enumerate all types for '{0}'.", fileName));

            if (!e.LoaderExceptions.Any())
            {
                sb.AppendLine(string.Format("Exception message: {0}", e));
                return sb.ToString();
            }

            var nsbAssemblyName = typeof(AssemblyScanner).Assembly.GetName();
            var nsbPublicKeyToken = BitConverter.ToString(nsbAssemblyName.GetPublicKeyToken()).Replace("-", "").ToLowerInvariant();
            var displayBindingRedirects = false;
            var files = new List<string>();
            var sbFileLoadException = new StringBuilder();
            var sbGenericException = new StringBuilder();

            foreach (var ex in e.LoaderExceptions)
            {
                var loadException = ex as FileLoadException;

                if (loadException != null)
                {
                    var assemblyName = new AssemblyName(loadException.FileName);
                    var assemblyPublicKeyToken = BitConverter.ToString(assemblyName.GetPublicKeyToken()).Replace("-", "").ToLowerInvariant();
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
                sb.AppendLine("It looks like you may be missing binding redirects in your config file for the following assemblies:");
                sb.Append(sbFileLoadException);
                sb.AppendLine("For more information see http://msdn.microsoft.com/en-us/library/7wd6ex19(v=vs.100).aspx");
            }

            if (displayBindingRedirects)
            {
                sb.AppendLine();
                sb.AppendLine("Try to add the following binding redirects to your config file:");

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

        IEnumerable<FileInfo> ScanDirectoryForAssemblyFiles()
        {
            var baseDir = new DirectoryInfo(baseDirectoryToScan);
            var searchOption = ScanNestedDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return GetFileSearchPatternsToUse()
                .SelectMany(extension => baseDir.GetFiles(extension, searchOption));
        }

        IEnumerable<string> GetFileSearchPatternsToUse()
        {
            yield return "*.dll";

            if (IncludeExesInScan)
            {
                yield return "*.exe";
            }
        }

        bool AssemblyPassesReferencesTest(string assemblyPath)
        {
            if (MustReferenceAtLeastOneAssembly.Count == 0)
            {
                return true;
            }
            var lightLoad = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            var referencedAssemblies = lightLoad.GetReferencedAssemblies();

            return MustReferenceAtLeastOneAssembly
                .Select(reference => reference.GetName().Name)
                .Any(name => referencedAssemblies.Any(a => a.Name == name));
        }

        internal static bool IsInDefaultAssemblyExclusions(string assemblyNameOrFileName)
        {
            var isExcludedByDefault = DefaultAssemblyExclusions.Any(exclusion => IsMatch(exclusion, assemblyNameOrFileName));
            return isExcludedByDefault;
        }

        /// <summary>
        ///  Determines whether the specified assembly name or file name can be included, given the set up include/exclude
        ///  patterns and default include/exclude patterns
        /// </summary>
        bool IsIncluded(string assemblyNameOrFileName)
        {
            var isExplicitlyExcluded = AssembliesToSkip.Any(excluded => IsMatch(excluded, assemblyNameOrFileName));
            if (isExplicitlyExcluded)
            {
                return false;
            }

            if (IsInDefaultAssemblyExclusions(assemblyNameOrFileName))
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

        internal static bool IsAllowedType(Type type)
        {
            return type != null &&
                   !type.IsValueType &&
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

        string baseDirectoryToScan;
        internal List<string> AssembliesToInclude;
        internal List<string> AssembliesToSkip;

        internal bool IncludeExesInScan = true;
        internal bool ScanNestedDirectories = true;

        //TODO: delete when we make message scanning lazy #1617
        static string[] DefaultAssemblyExclusions =
                                {

                                    "system.",
                                    "mscorlib.", 
                  
                                    // NSB Build-Dependencies
                                    "nunit.", "pnunit.", "rhino.mocks.", "XsdGenerator.",
                 
                                    // NSB OSS Dependencies
                                    "rhino.licensing.", "bouncycastle.crypto",
                                    "magnum.", "interop.", "nlog.", "newtonsoft.json.",
                                    "common.logging.", "topshelf.",
                                    "Autofac.", "log4net.", "nhibernate.", 
                                    "castle.",

                                    // Raven
                                    "raven.server", "raven.client", "raven.munin.",
                                    "raven.storage.", "raven.abstractions.", "raven.database",
                                    "esent.interop", "asyncctplibrary.", "lucene.net.",
                                    "icsharpcode.nrefactory", "spatial4n.core",

                                    // Azure host process, which is typically referenced for ease of deployment but should not be scanned
                                    "NServiceBus.Hosting.Azure.HostProcess.exe",

                                    // And other windows azure stuff
                                    "Microsoft.WindowsAzure."

                                };
    }
}