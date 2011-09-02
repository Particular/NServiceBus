using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class OnAzureStorageProfileHandler : IHandleProfile<OnAzureStorage>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .AzureSubcriptionStorage()
                .Sagas().AzureSagaPersister().NHibernateUnitOfWork();

        }
    }
}