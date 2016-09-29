namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Hosting;
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
                    {"ProcessID", Process.GetCurrentProcess().Id.ToString()},
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
        }

        internal const string HostIdSettingsKey = "NServiceBus.HostInformation.HostId";
    }
}