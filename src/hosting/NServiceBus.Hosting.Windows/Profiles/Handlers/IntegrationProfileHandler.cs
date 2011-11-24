namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Hosting.Profiles;
    using Unicast.Subscriptions;

    internal class IntegrationProfileHandler : IHandleProfile<Integration>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.MessageForwardingInCaseOfFault();


            if (Config is AsA_Publisher)
            {
                if (!Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
                {
                    Configure.Instance.MsmqSubscriptionStorage();
                }
            }
        }

        public IConfigureThisEndpoint Config { get; set; }
        
    }
}