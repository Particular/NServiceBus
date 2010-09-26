using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class ProductionProfileHandler : IHandleProfile<Production>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            //Configure.Instance
            //    .NHibernateSagaPersister();

            //Configure.Instance.MessageForwardingInCaseOfFault();

            if (Config is AsA_Publisher)
            {
                const bool createSchema = true;
                Configure.Instance.NHibernateAzureSubcriptionStorage(createSchema);
            }
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}