#nullable enable
namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Settings;
using Support;

partial class HostingComponent
{
    public class Settings
    {
        public Settings(SettingsHolder settings)
        {
            this.settings = settings;

            fullPathToStartingExe = PathUtilities.SanitizedPath(Environment.CommandLine);

            settings.SetDefault(DisplayNameSettingsKey, RuntimeEnvironment.MachineName);
            settings.SetDefault(PropertiesSettingsKey, new Dictionary<string, string>
            {
                {"Machine", RuntimeEnvironment.MachineName},
                {"ProcessID", Environment.ProcessId.ToString()},
                {"UserName", Environment.UserName},
                {"PathToExecutable", fullPathToStartingExe}
            });

            settings.Set(new StartupDiagnosticEntries());
            settings.Set(new ManifestItems());
        }

        public Guid HostId
        {
            get => settings.Get<Guid>(HostIdSettingsKey);
            set => settings.Set(HostIdSettingsKey, value);
        }

        public string DisplayName
        {
            get => settings.Get<string>(DisplayNameSettingsKey);
            set => settings.Set(DisplayNameSettingsKey, value);
        }

        public string EndpointName => settings.EndpointName();

        public string Discriminator => settings.GetOrDefault<string>("EndpointInstanceDiscriminator");

        public Dictionary<string, string> Properties
        {
            get => settings.Get<Dictionary<string, string>>(PropertiesSettingsKey);
            set => settings.Set(PropertiesSettingsKey, value);
        }

        public StartupDiagnosticEntries StartupDiagnostics => settings.Get<StartupDiagnosticEntries>();

        public string? DiagnosticsPath
        {
            get => settings.GetOrDefault<string>(DiagnosticsPathSettingsKey);
            set => settings.Set(DiagnosticsPathSettingsKey, value);
        }

        public Func<string, CancellationToken, Task>? HostDiagnosticsWriter
        {
            get => settings.GetOrDefault<Func<string, CancellationToken, Task>>(HostDiagnosticsWriterSettingsKey);
            set => settings.Set(HostDiagnosticsWriterSettingsKey, value);
        }

        public Func<string, CancellationToken, Task>? EndpointManifestWriter
        {
            get => settings.GetOrDefault<Func<string, CancellationToken, Task>>(EndpointManifestWriterSettingsKey);
            set => settings.Set(EndpointManifestWriterSettingsKey, value);
        }

        public Func<ICriticalErrorContext, CancellationToken, Task>? CustomCriticalErrorAction
        {
            get => settings.GetOrDefault<Func<ICriticalErrorContext, CancellationToken, Task>>(CustomCriticalErrorActionSettingsKey);
            set => settings.Set(CustomCriticalErrorActionSettingsKey, value);
        }

        public List<Action<IServiceCollection>> UserRegistrations { get; } = [];

        public string? InstallationUserName
        {
            get => settings.GetOrDefault<string>("Installers.UserName");
            set => settings.Set("Installers.UserName", value);
        }

        public bool ShouldRunInstallers
        {
            get => settings.GetOrDefault<bool>("Installers.Enable");
            set => settings.Set("Installers.Enable", value);
        }

        public string? ManifestOutputPath
        {
            get => settings.GetOrDefault<string>("Manifest.Path");
            set => settings.Set("Manifest.Path", value);
        }

        public bool ShouldGenerateManifest
        {
            get => settings.GetOrDefault<bool>("Manifest.Enable");
            set => settings.Set("Manifest.Enable", value);
        }

        public ManifestItems Manifest => settings.Get<ManifestItems>();

        public bool EnableOpenTelemetry { get; set; }

        internal void ApplyHostIdDefaultIfNeeded()
        {
            // We don't want to do settings.SetDefault() all the time, because the default uses MD5 which runs afoul of FIPS in such a way that cannot be worked around.
            // Changing the default implementation to a FIPS-compliant cipher would cause all users to get duplicates of every endpoint instance in ServicePulse.
            if (settings.HasExplicitValue(HostIdSettingsKey))
            {
                return;
            }

            settings.Set(HostIdSettingsKey, DeterministicGuid.Create(fullPathToStartingExe, RuntimeEnvironment.MachineName));
        }

        internal void UpdateHost(string hostName)
        {
            RuntimeEnvironment.SetMachineName(hostName);
            settings.Set(HostIdSettingsKey, DeterministicGuid.Create(fullPathToStartingExe, hostName));
            Properties["Machine"] = hostName;
            settings.SetDefault(DisplayNameSettingsKey, hostName);
        }

        readonly SettingsHolder settings;
        readonly string fullPathToStartingExe;

        const string HostIdSettingsKey = "NServiceBus.HostInformation.HostId";
        const string DisplayNameSettingsKey = "NServiceBus.HostInformation.DisplayName";
        const string PropertiesSettingsKey = "NServiceBus.HostInformation.Properties";
        const string DiagnosticsPathSettingsKey = "Diagnostics.RootPath";
        const string HostDiagnosticsWriterSettingsKey = "HostDiagnosticsWriter";
        const string EndpointManifestWriterSettingsKey = "EndpointManifestWriter";
        const string CustomCriticalErrorActionSettingsKey = "onCriticalErrorAction";
    }
}