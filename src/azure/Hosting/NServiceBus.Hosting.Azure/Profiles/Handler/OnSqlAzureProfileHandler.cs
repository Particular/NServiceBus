using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class OnSqlAzureProfileHandler : IHandleProfile<OnSqlAzure>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            if (Config is AsA_Worker)
            {
                Configure.Instance
                    .UseNHibernateSubscriptionPersister()
                    .Sagas().NHibernateSagaPersister().NHibernateUnitOfWork();
            }

        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}