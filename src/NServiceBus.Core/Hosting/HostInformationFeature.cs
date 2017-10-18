namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Runtime;
    using Hosting;
    using Settings;
    using Support;

    class HostInformationFeature : Feature
    {
        public HostInformationFeature()
        {
            EnableByDefault();
            Defaults(s =>
            {
                var fullPathToStartingExe = PathUtilities.SanitizedPath(Environment.CommandLine);

                if (!s.HasExplicitValue(HostIdSettingsKey))
                {
                    s.SetDefault(HostIdSettingsKey, DeterministicGuid.Create(fullPathToStartingExe, RuntimeEnvironment.MachineName));
                }
                s.SetDefault("NServiceBus.HostInformation.DisplayName", RuntimeEnvironment.MachineName);
                s.SetDefault("NServiceBus.HostInformation.Properties", new Dictionary<string, string>
                {
                    {"Machine", RuntimeEnvironment.MachineName},
#pragma warning disable PC001
                    {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
#pragma warning restore PC001
                    {"UserName", Environment.UserName},
                    {"PathToExecutable", fullPathToStartingExe}
                });
            });
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var hostInformation = new HostInformation(context.Settings.Get<Guid>(HostIdSettingsKey),
                context.Settings.Get<string>("NServiceBus.HostInformation.DisplayName"),
                context.Settings.Get<Dictionary<string, string>>("NServiceBus.HostInformation.Properties"));

            context.Container.ConfigureComponent(() => hostInformation, DependencyLifecycle.SingleInstance);

            var endpointName = context.Settings.EndpointName();
            context.Pipeline.Register("AuditHostInformation", new AuditHostInformationBehavior(hostInformation, endpointName), "Adds audit host information");
            context.Pipeline.Register("AddHostInfoHeaders", new AddHostInfoHeadersBehavior(hostInformation, endpointName), "Adds host info headers to outgoing headers");

            AddDiagnostics(context.Settings, hostInformation);
        }

        static void AddDiagnostics(ReadOnlySettings settings, HostInformation hostInformation)
        {
            settings.AddStartupDiagnosticsSection("Hosting", new
            {
                hostInformation.HostId,
                HostDisplayName = hostInformation.DisplayName,
                RuntimeEnvironment.MachineName,
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
        }

        internal const string HostIdSettingsKey = "NServiceBus.HostInformation.HostId";
    }
}