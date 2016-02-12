namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using NServiceBus.Hosting;
    using NServiceBus.Support;

    class HostInformationFeature : Feature
    {
        internal const string HostIdSettingsKey = "NServiceBus.HostInformation.HostId";

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

            context.Pipeline.Register("AuditHostInformation", typeof(AuditHostInformationBehavior), "Adds audit host information");
            context.Pipeline.Register("FaultHostInformation", typeof(FaultHostInformationBehavior), "Adds fault host information");
            context.Pipeline.Register("AddHostInfoHeaders", typeof(AddHostInfoHeadersBehavior), "Adds host info headers to outgoing headers");

            context.Container.ConfigureComponent(b => new AddHostInfoHeadersBehavior(hostInformation, context.Settings.EndpointName()), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new AuditHostInformationBehavior(hostInformation, context.Settings.EndpointName()), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new FaultHostInformationBehavior(hostInformation, context.Settings.EndpointName()), DependencyLifecycle.SingleInstance);
        }
    }
}