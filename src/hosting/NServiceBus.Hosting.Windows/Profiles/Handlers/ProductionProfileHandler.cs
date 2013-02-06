namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Hosting.Profiles;
    using Saga;
    using Unicast.Subscriptions;

    internal class ProductionProfileHandler : IHandleProfile<Production>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<ISagaPersister>())
            {
                Configure.Instance.RavenSagaPersister();
            }

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
            {
                Configure.Instance.MessageForwardingInCaseOfFault();
            }

            if (Config is AsA_Publisher && !Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
            {
                Configure.Instance.RavenSubscriptionStorage();
            }
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}