using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class OnSqlAzureProfileHandler : IHandleProfile<OnSqlAzure>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .DBSubcriptionStorage()
                .Sagas().NHibernateSagaPersister().NHibernateUnitOfWork();

        }
    }
}