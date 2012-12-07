namespace NServiceBus
{
    using System;

    using Apache.NMS;
    using Apache.NMS.ActiveMQ;

    using NServiceBus.ActiveMQ;
    using NServiceBus.Config;
    using NServiceBus.Unicast.Queuing.Installers;

    public static class ConfigureActiveMqMessageQueue
    {
        /// <summary>
        /// Configures ActiveMq as the transport. Settings are read from the configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure ActiveMqTransport(this Configure config)
        {
            var configSection = Configure.GetConfigSection<ActiveMqTransportConfig>();

            return ActiveMqTransport(config, configSection.BrokerUri);
        }

        /// <summary>
        /// Use MSMQ for your queuing infrastructure.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="brokerUri">Address of the ActiveMQ broker to connect to</param>
        /// <returns></returns>
        public static Configure ActiveMqTransport(this Configure config, string brokerUri)
        {
            config.Configurer.ConfigureComponent<ActiveMqMessageReceiver>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested)
                .ConfigureProperty(p => p.ConsumerName, Configure.EndpointName);

            config.Configurer.ConfigureComponent<ActiveMqMessageSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqSubscriptionStorage>(DependencyLifecycle.InstancePerCall);
            
            config.Configurer.ConfigureComponent<SubscriptionManager>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<ActiveMqMessageMapper>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MessageTypeInterpreter>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<TopicEvaluator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<DestinationEvaluator>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<ActiveMqQueueCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqMessageDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<NotifyMessageReceivedFactory>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqPurger>(DependencyLifecycle.SingleInstance);

            var factory = new NetTxConnectionFactory(brokerUri)
                {
                    AcknowledgementMode = AcknowledgementMode.Transactional,
                    PrefetchPolicy = { QueuePrefetch = 1 }
                };

            config.Configurer.ConfigureComponent<INetTxConnectionFactory>(() => factory, DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent(() => (INetTxConnection)factory.CreateConnection(), DependencyLifecycle.SingleInstance);

            EndpointInputQueueCreator.Enabled = true;

            return config;
        }
    }
}
