﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Runtime;
    using Hosting;
    using Settings;
    using Support;

    class HostingComponent
    {
        HostingComponent(HostInformation hostInformation)
        {
            HostInformation = hostInformation;
        }

        public static HostingComponent Initialize(Configuration configuration,
            ContainerComponent containerComponent,
            PipelineComponent pipelineComponent,
            string endpointName,
            ReadOnlySettings settings)
        {
            var hostInformation = new HostInformation(configuration.HostId, configuration.DisplayName, configuration.Properties);

            //for backwards compatibility, can be removed in v8
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

            return new HostingComponent(hostInformation);
        }

        public HostInformation HostInformation { get; }

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
        }
    }
}