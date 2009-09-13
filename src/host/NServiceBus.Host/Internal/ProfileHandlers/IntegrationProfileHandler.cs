using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Integration profile.
    /// </summary>
    public class IntegrationProfileHandler : IConfigureTheBusForProfile<Integration>
    {
        void IConfigureTheBus.Configure(IConfigureThisEndpoint specifier)
        {
            var busConfiguration = 
                NServiceBus.Configure.With()
                .SpringBuilder()
                .XmlSerializer()
                .Sagas()
                .NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration();

            if (specifier is AsA_Publisher)
            {
                if (Configure.GetConfigSection<MsmqSubscriptionStorageConfig>() == null)
                    busConfiguration.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(
                        ComponentCallModelEnum.Singleton)
                        .ConfigureProperty(s => s.Queue, Program.EndpointId + "_subscriptions");
                else
                    busConfiguration.MsmqSubscriptionStorage();
            }
        }
    }
}
