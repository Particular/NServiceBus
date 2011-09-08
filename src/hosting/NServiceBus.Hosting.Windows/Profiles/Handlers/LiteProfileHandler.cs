using System;
using NServiceBus.Config;
using NServiceBus.Faults;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Saga;
using NServiceBus.Unicast.Subscriptions;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    internal class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig, IWantToRunWhenConfigurationIsComplete
    {
        void IHandleProfile.ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<ISagaPersister>())
                Configure.Instance.InMemorySagaPersister();

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.InMemoryFaultManagement();

            if (Config is AsA_Publisher)
                if (!Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
                    Configure.Instance.InMemorySubscriptionStorage();
        }

        public void Run()
        {
            Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}