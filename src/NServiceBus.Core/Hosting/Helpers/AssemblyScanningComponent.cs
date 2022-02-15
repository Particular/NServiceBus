namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Settings;
    using Hosting.Helpers;
    using System.Reflection;
    using NServiceBus.Logging;

    class AssemblyScanningComponent
    {
        public static AssemblyScanningComponent Initialize(Configuration configuration, SettingsHolder settings)
        {
            var shouldScanAssemblies = configuration.UserProvidedTypes == null;

            List<Type> availableTypes;
            AssemblyScanner assemblyScanner;

            if (shouldScanAssemblies)
            {
                var directoryToScan = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;

                assemblyScanner = new AssemblyScanner(directoryToScan);
                availableTypes = new List<Type>();
            }
            else
            {
                assemblyScanner = new AssemblyScanner(Assembly.GetExecutingAssembly());
                availableTypes = configuration.UserProvidedTypes;
            }

            var assemblyScannerSettings = configuration.AssemblyScannerConfiguration;

            assemblyScanner.AssembliesToSkip = assemblyScannerSettings.ExcludedAssemblies;
            assemblyScanner.TypesToSkip = assemblyScannerSettings.ExcludedTypes;
            assemblyScanner.ScanNestedDirectories = assemblyScannerSettings.ScanAssembliesInNestedDirectories;
            assemblyScanner.ThrowExceptions = assemblyScannerSettings.ThrowExceptions;
            assemblyScanner.ScanFileSystemAssemblies = assemblyScannerSettings.ScanFileSystemAssemblies;
            assemblyScanner.ScanAppDomainAssemblies = assemblyScannerSettings.ScanAppDomainAssemblies;
            assemblyScanner.AdditionalAssemblyScanningPath = assemblyScannerSettings.AdditionalAssemblyScanningPath;

            if (!assemblyScanner.ScanAppDomainAssemblies && !assemblyScanner.ScanFileSystemAssemblies)
            {
                Logger.Warn($"Assembly scanning have been disabled. Message handlers, features and installers might not be loaded. Enable {assemblyScannerSettings.ScanAppDomainAssemblies} and/or {assemblyScannerSettings.ScanFileSystemAssemblies} to automatically load handlers, features and installers.");
            }

            var scannableAssemblies = assemblyScanner.GetScannableAssemblies();
            availableTypes = scannableAssemblies.Types.Union(availableTypes).ToList();

            configuration.SetDefaultAvailableTypes(availableTypes);

            if (shouldScanAssemblies)
            {
                settings.AddStartupDiagnosticsSection("AssemblyScanning", new
                {
                    Assemblies = scannableAssemblies.Assemblies.Select(a => new
                    {
                        a.FullName,
                        FileVersion = FileVersionRetriever.GetFileVersion(a)
                    }),
                    scannableAssemblies.ErrorsThrownDuringScanning,
                    scannableAssemblies.SkippedFiles,
                    Settings = assemblyScannerSettings
                });
            }

            return new AssemblyScanningComponent(availableTypes);
        }

        AssemblyScanningComponent(List<Type> availableTypes)
        {
            AvailableTypes = availableTypes;
        }

        public List<Type> AvailableTypes { get; }

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
            {
                this.settings = settings;
            }

            public List<Type> UserProvidedTypes { get; set; }

            public AssemblyScannerConfiguration AssemblyScannerConfiguration => settings.GetOrCreate<AssemblyScannerConfiguration>();

            public IList<Type> AvailableTypes => settings.Get<IList<Type>>(TypesToScanSettingsKey);

            public void SetDefaultAvailableTypes(IList<Type> scannedTypes)
            {
                settings.SetDefault(TypesToScanSettingsKey, scannedTypes);
            }

            SettingsHolder settings;

            static string TypesToScanSettingsKey = "TypesToScan";
        }

        static ILog Logger = LogManager.GetLogger<AssemblyScanningComponent>();
    }
}
