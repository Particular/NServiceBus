namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Security.Principal;
    using System.Threading;
    using Hosting;
    using Support;
    using Utils;

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

            context.Container.RegisterSingleton(hostInformation);

            context.Pipeline.Register("AuditHostInformation", typeof(AuditHostInformationBehavior), "Adds audit host information");
            context.Pipeline.Register("AddHostInfoHeaders", typeof(AddHostInfoHeadersBehavior), "Adds host info headers to outgoing headers");

            context.Container.ConfigureComponent(b => new AddHostInfoHeadersBehavior(hostInformation, context.Settings.EndpointName()), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new AuditHostInformationBehavior(hostInformation, context.Settings.EndpointName()), DependencyLifecycle.SingleInstance);

            string identity;

            if (!TryGetCurrentIdentity(out identity))
            {
                return;
            }

            context.Pipeline.Register("AddWindowsIdentityHeader", typeof(AddWindowsIdentityHeaderBehavior), "Adds the identity of the current thread outgoing headers");
            context.Container.ConfigureComponent(b => new AddWindowsIdentityHeaderBehavior(identity), DependencyLifecycle.SingleInstance);
        }

        static bool TryGetCurrentIdentity(out string identity)
        {
            if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
            {
                identity = Thread.CurrentPrincipal.Identity.Name;

                return true;
            }
            using (var windowsIdentity = WindowsIdentity.GetCurrent())
            {
                if (windowsIdentity == null)
                {
                    identity = null;
                    return false;
                }

                identity = windowsIdentity.Name;
                return true;
            }
        }
    }
}