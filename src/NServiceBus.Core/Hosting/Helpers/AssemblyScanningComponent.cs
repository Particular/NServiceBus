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
            return new AssemblyScanningComponent([], configuration.StrictRegisteredOnlyMode);
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

        // Deliberately strongly typed because we need to truncate this super large section when writing to the logs
        var assemblyScanningDiagnostics = new AssemblyScanningDiagnostics(
            scannableAssemblies.Assemblies.Select(a => new AssemblyDetails(a.FullName ?? "Unknown assembly", FileVersionRetriever.GetFileVersion(a))),
            scannableAssemblies.SkippedFiles.Select(f => new SkippedFile(f.FilePath, f.SkipReason)),
            scannableAssemblies.ErrorsThrownDuringScanning,
            assemblyScannerSettings
        );

        settings.AddStartupDiagnosticsSection(AssemblyScanningDiagnostics.SectionName, assemblyScanningDiagnostics, StartupDiagnosticsJsonContext.Default.AssemblyScanningDiagnostics);

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

    AssemblyScanningComponent(IList<Type> availableTypes, bool isStrictRegisteredOnlyMode = false)
    {
        AvailableTypes = availableTypes;
        IsStrictRegisteredOnlyMode = isStrictRegisteredOnlyMode;
    }

    public IList<Type> AvailableTypes { get; }

    /// <summary>
    /// When assembly scanning is disabled and the application is trimmed or dynamic code is unavailable, the endpoint
    /// operates in strict registered-only message metadata mode: metadata is only resolved for types registered up
    /// front (e.g. via the source-generated <c>AddMessageType&lt;T&gt;</c> registration) and no runtime registration or
    /// dynamic type loading is performed.
    /// </summary>
    public bool IsStrictRegisteredOnlyMode { get; }

    public class Configuration(SettingsHolder settings)
    {
        // Testability hook until a dedicated AOT test project exists.
        public bool DynamicCodeSupported { get; set; } = System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;

        // Mirrors the NServiceBus.EnableStrictRegisteredOnlyMessageMetadata runtime switch emitted by the
        // build-transitive NServiceBus.targets for trimmed/AOT executables. Testability only; production code
        // never assigns to it.
        public bool StrictRegisteredOnlyMessageMetadataEnabled { get; set; } = AppContextSwitches.IsStrictRegisteredOnlyMessageMetadataEnabled;

        // The computed default only matters when scanning is disabled; EndpointCreator overwrites it with the
        // component's decision after Initialize has run.
        public bool StrictRegisteredOnlyMode
        {
            get => strictRegisteredOnlyMode ?? (StrictRegisteredOnlyMessageMetadataEnabled || !DynamicCodeSupported);
            set => strictRegisteredOnlyMode = value;
        }

        bool? strictRegisteredOnlyMode;

        public List<Type>? UserProvidedTypes { get; set; }

        public AssemblyScannerConfiguration AssemblyScannerConfiguration => settings.GetOrCreate<AssemblyScannerConfiguration>();

        public IList<Type> AvailableTypes => settings.Get<IList<Type>>(TypesToScanSettingsKey);

        public void SetDefaultAvailableTypes(IList<Type> scannedTypes) => settings.SetDefault(TypesToScanSettingsKey, scannedTypes);

        static readonly string TypesToScanSettingsKey = "TypesToScan";
    }
}