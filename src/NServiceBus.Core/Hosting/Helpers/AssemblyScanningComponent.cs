#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hosting.Helpers;
using Settings;

class AssemblyScanningComponent
{
    public static AssemblyScanningComponent Initialize(Configuration configuration, SettingsHolder settings)
    {
        if (configuration.AssemblyScannerConfiguration.Disable)
        {
            return new AssemblyScanningComponent([]);
        }

        if (!configuration.DynamicCodeSupported)
        {
            throw new Exception("Assembly scanning requires to access unreferenced and dynamic code which is not supported on this system. Please disable assembly scanning and add manual registrations for handler, sagas, etc. using the corresponding APIs");
        }

        if (configuration.UserProvidedTypes != null)
        {
            return new AssemblyScanningComponent(configuration.UserProvidedTypes);
        }

        var assemblyScannerSettings = configuration.AssemblyScannerConfiguration;

        if (!assemblyScannerSettings.ScanAppDomainAssemblies && !assemblyScannerSettings.ScanFileSystemAssemblies)
        {
            throw new Exception($"Both file and appdomain scanning has been turned off which results in no assemblies being scanned. Enable `{nameof(AssemblyScannerConfiguration.ScanAppDomainAssemblies)}` or `{nameof(AssemblyScannerConfiguration.ScanFileSystemAssemblies)}` to scan assemblies, or set `{nameof(AssemblyScannerConfiguration.Disable)}` to 'true' to explicitly disable assembly scanning.");
        }

        var scannableAssemblies = ScanAssemblies(assemblyScannerSettings);
        var availableTypes = scannableAssemblies.Types.ToList();

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

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Method will not be called if dynamic code is not supported")]
    static AssemblyScannerResults ScanAssemblies(AssemblyScannerConfiguration assemblyScannerSettings)
    {
        var directoryToScan = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;

        var assemblyScanner = new AssemblyScanner(directoryToScan)
        {
            AssembliesToSkip = assemblyScannerSettings.ExcludedAssemblies,
            TypesToSkip = assemblyScannerSettings.ExcludedTypes,
            ScanNestedDirectories = assemblyScannerSettings.ScanAssembliesInNestedDirectories,
            ThrowExceptions = assemblyScannerSettings.ThrowExceptions,
            ScanFileSystemAssemblies = assemblyScannerSettings.ScanFileSystemAssemblies,
            ScanAppDomainAssemblies = assemblyScannerSettings.ScanAppDomainAssemblies,
            AdditionalAssemblyScanningPath = assemblyScannerSettings.AdditionalAssemblyScanningPath
        };

        return assemblyScanner.GetScannableAssemblies();
    }

    AssemblyScanningComponent(IList<Type> availableTypes) => AvailableTypes = availableTypes;

    public IList<Type> AvailableTypes { get; }

    public class Configuration(SettingsHolder settings)
    {
        // This is only used for testability until we have a dedicated AOT test project. Not called on the hot path so has no performance implications. 
        public bool DynamicCodeSupported { get; set; } = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;

        public List<Type>? UserProvidedTypes { get; set; }

        public AssemblyScannerConfiguration AssemblyScannerConfiguration => settings.GetOrCreate<AssemblyScannerConfiguration>();

        public IList<Type> AvailableTypes => settings.Get<IList<Type>>(TypesToScanSettingsKey);

        public void SetDefaultAvailableTypes(IList<Type> scannedTypes) => settings.SetDefault(TypesToScanSettingsKey, scannedTypes);

        static readonly string TypesToScanSettingsKey = "TypesToScan";
    }
}