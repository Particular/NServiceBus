using System.Collections.Specialized;
using Common.Logging;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Integration profile.
    /// </summary>
    public class IntegrationProfileHandler : IHandleProfileConfiguration<Integration>
    {
        private IConfigureThisEndpoint spec;

        void IHandleProfileConfiguration.Init(IConfigureThisEndpoint specifier)
        {
            spec = specifier;
        }

        void IHandleProfileConfiguration.ConfigureSagas(Configure busConfiguration)
        {
            if (!(spec is ISpecify.MyOwn.SagaPersistence))
                busConfiguration.NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration();
        }

        void IHandleProfileConfiguration.ConfigureSubscriptionStorage(Configure busConfiguration)
        {

            if (Configure.GetConfigSection<MsmqSubscriptionStorageConfig>() == null)
            {
				string q = Program.EndpointId + "_subscriptions";
                busConfiguration.Configurer.ConfigureComponent<MsmqSubscriptionStorage>(ComponentCallModelEnum.Singleton)
                    .ConfigureProperty(s => s.Queue, q);
            }
            else
            {
                busConfiguration.MsmqSubscriptionStorage();
            }
        }
    }
}
