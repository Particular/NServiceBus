namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Hosting.Profiles;
    using Saga;
    using Unicast.Subscriptions;

    internal class IntegrationProfileHandler : IHandleProfile<Integration>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RavenPersistence();

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

            WindowsInstallerRunner.RunInstallers = true;
            WindowsInstallerRunner.RunInfrastructureInstallers = false;
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}