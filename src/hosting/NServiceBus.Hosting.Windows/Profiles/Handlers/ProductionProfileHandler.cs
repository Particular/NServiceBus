using NServiceBus.Faults;
using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Persistence.Raven;
    using Persistence.Raven.Config;

    internal class ProductionProfileHandler : IHandleProfile<Production>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RavenPersistence();

            //todo
            //Configure.Instance.RavenSagaPersister();

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.MessageForwardingInCaseOfFault();

            if (Config is AsA_Publisher)
                Configure.Instance.RavenSubscriptionStorage(Program.EndpointId);
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}