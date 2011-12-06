using NServiceBus.Faults;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Saga;
using NServiceBus.Unicast.Subscriptions;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    internal class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.AsMasterNode()
                .UseTimeoutManagerWithInMemoryPersistence()
                .GatewayWithInMemoryPersistence();

            if (!Configure.Instance.Configurer.HasComponent<ISagaPersister>())
                Configure.Instance.InMemorySagaPersister();

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.InMemoryFaultManagement();

            if (Config is AsA_Publisher)
                if (!Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
                    Configure.Instance.InMemorySubscriptionStorage();

            WindowsInstallerRunner.RunInstallers = true;
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}