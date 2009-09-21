using System;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Subscriptions.Msmq;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Configures the infrastructure for the Integration profile.
    /// </summary>
    public class IntegrationProfileHandler : IHandleProfile<Integration>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            NServiceBus.Configure.Instance
                .NHibernateSagaPersisterWithSQLiteAndAutomaticSchemaGeneration();

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
