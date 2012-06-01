using NServiceBus.Faults;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Unicast.Subscriptions;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Saga;

    internal class ProductionProfileHandler : IHandleProfile<Production>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RavenPersistence();

            if (!Configure.Instance.Configurer.HasComponent<ISagaPersister>())
                Configure.Instance.RavenSagaPersister();

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.MessageForwardingInCaseOfFault();

            if (Config is AsA_Publisher && !Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
                Configure.Instance.RavenSubscriptionStorage();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}