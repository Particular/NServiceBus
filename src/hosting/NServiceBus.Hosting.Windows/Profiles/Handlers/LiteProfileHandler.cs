using NServiceBus.Faults;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Unicast.Subscriptions;


namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Persistence.InMemory;

    internal class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            InMemoryPersistence.UseAsDefault();

            Configure.Instance.AsMasterNode()
                .DefaultToInMemoryGatewayPersistence();

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