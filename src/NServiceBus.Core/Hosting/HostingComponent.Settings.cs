namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

            public string EndpointName
            {
                get { return settings.EndpointName(); }
            }

            public Dictionary<string, string> Properties
            {
                get { return settings.Get<Dictionary<string, string>>(PropertiesSettingsKey); }
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

            public List<Action<IServiceCollection>> UserRegistrations { get; } = new List<Action<IServiceCollection>>();

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