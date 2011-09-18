using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class OnAzureTableStorageProfileHandler : IHandleProfile<OnAzureTableStorage>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            if (Config is AsA_Worker)
            {
                Configure.Instance
                    .AzureSubcriptionStorage()
                    .AzureSagaPersister().NHibernateUnitOfWork();
            }

        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}