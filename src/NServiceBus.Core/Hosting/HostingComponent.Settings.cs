namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using ObjectBuilder.Common;
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
                    {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
                    {"UserName", Environment.UserName},
                    {"PathToExecutable", fullPathToStartingExe}
                });

                settings.Set(new StartupDiagnosticEntries());
            }

            public Guid HostId
            {
                get { return settings.Get<Guid>(HostIdSettingsKey); }
                set { settings.Set(HostIdSettingsKey, value); }
            }

            public string DisplayName
            {
                get { return settings.Get<string>(DisplayNameSettingsKey); }
                set { settings.Set(DisplayNameSettingsKey, value); }
            }

            public string EndpointName => settings.EndpointName();

            public Dictionary<string, string> Properties
            {
                get => settings.Get<Dictionary<string, string>>(PropertiesSettingsKey);
                set { settings.Set(PropertiesSettingsKey, value); }
            }

            public StartupDiagnosticEntries StartupDiagnostics
            {
                get { return settings.Get<StartupDiagnosticEntries>(); }
                set { settings.Set(value); }
            }

            public string DiagnosticsPath
            {
                get { return settings.GetOrDefault<string>(DiagnosticsPathSettingsKey); }
                set { settings.Set(DiagnosticsPathSettingsKey, value); }
            }

            public Func<string, Task> HostDiagnosticsWriter
            {
                get { return settings.GetOrDefault<Func<string, Task>>(HostDiagnosticsWriterSettingsKey); }
                set { settings.Set(HostDiagnosticsWriterSettingsKey, value); }
            }

            public Func<ICriticalErrorContext, Task> CustomCriticalErrorAction
            {
                get { return settings.GetOrDefault<Func<ICriticalErrorContext, Task>>(CustomCriticalErrorActionSettingsKey); }
                set { settings.Set(CustomCriticalErrorActionSettingsKey, value); }
            }

            public List<Action<IConfigureComponents>> UserRegistrations { get; } = new List<Action<IConfigureComponents>>();

            public IContainer CustomObjectBuilder { get; set; }

            public string InstallationUserName
            {
                get => settings.GetOrDefault<string>("Installers.UserName");
                set => settings.Set("Installers.UserName", value);
            }

            public bool ShouldRunInstallers
            {
                get => settings.GetOrDefault<bool>("Installers.Enable");
                set => settings.Set("Installers.Enable", value);
            }

            // Since the host id default is using MD5 which breaks MIPS compliant users we need to delay setting the default until users have a chance to override
            // via a custom feature to be backwards compatible.
            // For more details see the test: When_feature_overrides_hostid_from_feature_default
            // When this is removed in v8 downstreams can no longer rely on the setting to always be there
            [ObsoleteEx(RemoveInVersion = "8", TreatAsErrorFromVersion = "7")]
            internal void ApplyHostIdDefaultIfNeededForV7BackwardsCompatibility()
            {
                if (settings.HasExplicitValue(HostIdSettingsKey))
                {
                    return;
                }

                settings.SetDefault(HostIdSettingsKey, DeterministicGuid.Create(fullPathToStartingExe, RuntimeEnvironment.MachineName));
            }

            SettingsHolder settings;
            string fullPathToStartingExe;

            const string HostIdSettingsKey = "NServiceBus.HostInformation.HostId";
            const string DisplayNameSettingsKey = "NServiceBus.HostInformation.DisplayName";
            const string PropertiesSettingsKey = "NServiceBus.HostInformation.Properties";
            const string DiagnosticsPathSettingsKey = "Diagnostics.RootPath";
            const string HostDiagnosticsWriterSettingsKey = "HostDiagnosticsWriter";
            const string CustomCriticalErrorActionSettingsKey = "onCriticalErrorAction";
        }
    }
}