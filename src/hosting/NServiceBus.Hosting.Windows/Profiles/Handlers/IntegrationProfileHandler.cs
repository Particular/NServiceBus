namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Config;
    using Faults;
    using Hosting.Profiles;
    using Saga;
    using Unicast.Subscriptions;

    internal class IntegrationProfileHandler : IHandleProfile<Integration>, IWantTheEndpointConfig, IWantToRunWhenConfigurationIsComplete
    {
        void IHandleProfile.ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.MessageForwardingInCaseOfFault();

            if (!Configure.Instance.Configurer.HasComponent<ISagaPersister>())
                Configure.Instance.RavenSagaPersister();


            if (Config is AsA_Publisher)
            {
                if (!Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
                {
                    Configure.Instance.MsmqSubscriptionStorage();
                }
            }
        }

        public void Run()
        {
            Configure.Instance.ForInstallationOn<Installation.Environments.Windows>().Install();
        }


        public IConfigureThisEndpoint Config { get; set; }
        
    }
}