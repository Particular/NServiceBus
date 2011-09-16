using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class OnAzureTableStorageProfileHandler : IHandleProfile<OnAzureTableStorage>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .AzureSubcriptionStorage()
                .Sagas().AzureSagaPersister().NHibernateUnitOfWork();

        }
    }
}