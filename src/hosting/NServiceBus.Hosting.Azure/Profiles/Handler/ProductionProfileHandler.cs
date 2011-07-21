using NServiceBus.Config;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Integration.Azure;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class ProductionProfileHandler : IHandleProfile<Production>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .AzureSagaPersister().NHibernateUnitOfWork();

            //Configure.Instance.MessageForwardingInCaseOfFault();

            if (Config is AsA_Publisher)
            {
                Configure.Instance.AzureSubcriptionStorage();
            }
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}