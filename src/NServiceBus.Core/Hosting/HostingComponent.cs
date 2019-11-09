namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Runtime;
    using System.Threading.Tasks;
    using Hosting;
    using Settings;
    using Support;

    class HostingComponent
    {
        HostingComponent(HostInformation hostInformation, StartupDiagnosticEntries startupDiagnostics, HostStartupDiagnosticsWriter hostStartupDiagnosticsWriter)
        {
            HostInformation = hostInformation;
            this.startupDiagnostics = startupDiagnostics;
            this.hostStartupDiagnosticsWriter = hostStartupDiagnosticsWriter;
        }

        public static HostingComponent Initialize(Configuration configuration,
            ContainerComponent containerComponent,
            PipelineComponent pipelineComponent,
            string endpointName,
            ReadOnlySettings settings)
        {
            var hostInformation = new HostInformation(configuration.HostId, configuration.DisplayName, configuration.Properties);

            containerComponent.ContainerConfiguration.ConfigureComponent(() => hostInformation, DependencyLifecycle.SingleInstance);

            pipelineComponent.PipelineSettings.Register("AuditHostInformation", new AuditHostInformationBehavior(hostInformation, endpointName), "Adds audit host information");
            pipelineComponent.PipelineSettings.Register("AddHostInfoHeaders", new AddHostInfoHeadersBehavior(hostInformation, endpointName), "Adds host info headers to outgoing headers");

            settings.AddStartupDiagnosticsSection("Hosting", new
            {
                hostInformation.HostId,
                HostDisplayName = hostInformation.DisplayName,
                RuntimeEnvironment.MachineName,
                OSPlatform = Environment.OSVersion.Platform,
                OSVersion = Environment.OSVersion.VersionString,
                GCSettings.IsServerGC,
                GCLatencyMode = GCSettings.LatencyMode,
                Environment.ProcessorCount,
                Environment.Is64BitProcess,
                CLRVersion = Environment.Version,
                Environment.WorkingSet,
                Environment.SystemPageSize,
                HostName = Dns.GetHostName(),
                Environment.UserName,
                PathToExe = PathUtilities.SanitizedPath(Environment.CommandLine)
            });

            var hostStartupDiagnosticsWriter = HostStartupDiagnosticsWriterFactory.GetDiagnosticsWriter(configuration);


            return new HostingComponent(hostInformation, configuration.StartupDiagnostics, hostStartupDiagnosticsWriter);
        }

        public HostInformation HostInformation { get; }

        public Task Start()
        {
            return hostStartupDiagnosticsWriter.Write(startupDiagnostics.entries);
        }

        readonly StartupDiagnosticEntries startupDiagnostics;
        private readonly HostStartupDiagnosticsWriter hostStartupDiagnosticsWriter;

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
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
                get
                {
                    return settings.Get<Guid>(HostIdSettingsKey);
                }
                set
                {
                    settings.Set(HostIdSettingsKey, value);
                }
            }

            public string DisplayName
            {
                get
                {
                    return settings.Get<string>(DisplayNameSettingsKey);
                }
                set
                {
                    settings.Set(DisplayNameSettingsKey, value);
                }
            }

            public string EndpointName
            {
                get
                {
                    return settings.EndpointName();
                }
            }

            public Dictionary<string, string> Properties
            {
                get
                {
                    return settings.Get<Dictionary<string, string>>(PropertiesSettingsKey);
                }
                set
                {
                    settings.Set(PropertiesSettingsKey, value);
                }
            }

            public StartupDiagnosticEntries StartupDiagnostics
            {
                get
                {
                    return settings.Get<StartupDiagnosticEntries>();
                }
                set
                {
                    settings.Set(value);
                }
            }

            public string DiagnosticsPath
            {
                get
                {
                    return settings.GetOrDefault<string>(DiagnosticsPathSettingsKey);
                }
                set
                {
                    settings.Set(DiagnosticsPathSettingsKey, value);
                }
            }

            public Func<string, Task> HostDiagnosticsWriter
            {
                get
                {
                    return settings.GetOrDefault<Func<string, Task>>(HostDiagnosticsWriterSettingsKey);
                }
                set
                {
                    settings.Set(HostDiagnosticsWriterSettingsKey, value);
                }
            }

            // Since the host id default is using MD5 which breaks MIPS compliant users we need to delay setting the default until users have a chance to override
            // via a custom feature to be backwards compatible.
            // For more details see the test: When_feature_overrides_hostid_from_feature_default
            // When this is removed in v8 downstreams can no longer rely on the setting to always be there
            [ObsoleteEx(RemoveInVersion = "8", TreatAsErrorFromVersion = "7")]
            internal void ApplyHostIdDefaultIfNeeded()
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
        }
    }
}