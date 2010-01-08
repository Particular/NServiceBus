using NServiceBus.Config;
using NServiceBus.Hosting.Profiles;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    internal class IntegrationProfileHandler : IHandleProfile<Integration>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration();

            Configure.Instance.Configurer.ConfigureComponent<Faults.InMemory.FaultManager>(ComponentCallModelEnum.Singleton);

            if (Config is AsA_Publisher)
            {
                if (Configure.GetConfigSection<MsmqSubscriptionStorageConfig>() == null)
                    Configure.Instance.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(
                        ComponentCallModelEnum.Singleton)
                        .ConfigureProperty(s => s.Queue, Program.EndpointId + "_subscriptions");
                else
                    Configure.Instance.MsmqSubscriptionStorage();
            }
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}