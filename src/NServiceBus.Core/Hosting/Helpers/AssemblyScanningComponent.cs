#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Hosting.Helpers;
using Settings;

class AssemblyScanningComponent
{
    public static AssemblyScanningComponent Initialize(Configuration configuration, SettingsHolder settings)
    {
        if (configuration.UserProvidedTypes != null)
        {
            return new AssemblyScanningComponent(configuration.UserProvidedTypes);
        }

        if (!configuration.AssemblyScannerConfiguration.Enabled)
        {
            return new AssemblyScanningComponent([]);
        }

        var directoryToScan = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;

        var assemblyScanner = new AssemblyScanner(directoryToScan);
        var availableTypes = new List<Type>();

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
            throw new Exception(@$"Both file and appdomain scanning has been turned off which results in no assemblies being scanned. 
Set {nameof(AssemblyScannerConfiguration.Enabled)} or {nameof(AssemblyScannerConfiguration.ScanFileSystemAssemblies)} to enable scanning. Set {nameof(AssemblyScannerConfiguration.Enabled)} to false to disable assembly scanning");
        }

        var scannableAssemblies = assemblyScanner.GetScannableAssemblies();
        availableTypes = scannableAssemblies.Types.Union(availableTypes).ToList();

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

        return new AssemblyScanningComponent(availableTypes);
    }

    AssemblyScanningComponent(IList<Type> availableTypes) => AvailableTypes = availableTypes;

    public IList<Type> AvailableTypes { get; }

    public class Configuration(SettingsHolder settings)
    {
        public List<Type>? UserProvidedTypes { get; set; }

        public AssemblyScannerConfiguration AssemblyScannerConfiguration => settings.GetOrCreate<AssemblyScannerConfiguration>();

        public IList<Type> AvailableTypes => settings.Get<IList<Type>>(TypesToScanSettingsKey);

        public void SetDefaultAvailableTypes(IList<Type> scannedTypes) => settings.SetDefault(TypesToScanSettingsKey, scannedTypes);

        static readonly string TypesToScanSettingsKey = "TypesToScan";
    }
}