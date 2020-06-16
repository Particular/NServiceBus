namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Settings;
    using Hosting.Helpers;
    using System.Reflection;

    class AssemblyScanningComponent
    {
        public static AssemblyScanningComponent Initialize(Configuration configuration, SettingsHolder settings)
        {
            var shouldScanBinDirectory = configuration.UserProvidedTypes == null;

            List<Type> availableTypes;
            AssemblyScanner assemblyScanner;

            if (shouldScanBinDirectory)
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
            assemblyScanner.ScanAppDomainAssemblies = assemblyScannerSettings.ScanAppDomainAssemblies;
            assemblyScanner.AdditionalAssemblyScanningPath = assemblyScannerSettings.AdditionalAssemblyScanningPath;

            var scannableAssemblies = assemblyScanner.GetScannableAssemblies();

            availableTypes = availableTypes.Union(scannableAssemblies.Types).ToList();

            configuration.SetDefaultAvailableTypes(availableTypes);

            if (shouldScanBinDirectory)
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

            public AssemblyScannerConfiguration AssemblyScannerConfiguration { get { return settings.GetOrCreate<AssemblyScannerConfiguration>(); } }

            public IList<Type> AvailableTypes { get { return settings.Get<IList<Type>>(TypesToScanSettingsKey); } }

            public void SetDefaultAvailableTypes(IList<Type> scannedTypes)
            {
                settings.SetDefault(TypesToScanSettingsKey, scannedTypes);
            }

            SettingsHolder settings;

            static string TypesToScanSettingsKey = "TypesToScan";
        }
    }
}
