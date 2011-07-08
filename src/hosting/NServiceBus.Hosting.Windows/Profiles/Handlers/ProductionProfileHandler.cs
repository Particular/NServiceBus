using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    internal class ProductionProfileHandler : IHandleProfile<Production>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            //todo
            //Configure.Instance
             //   .RavenSagaPersister();

            Configure.Instance.MessageForwardingInCaseOfFault();

            //todo
            //if (Config is AsA_Publisher)
              //  Configure.Instance.RavenSubscriptionStoreage();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}