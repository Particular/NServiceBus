namespace NServiceBus.Transport.ActiveMQ.Config
{
    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using NServiceBus.Config;
    using Unicast.Queuing.Installers;
    using MessageProducer = NServiceBus.Transport.ActiveMQ.MessageProducer;

    /// <summary>
    /// Default configuration for ActiveMQ
    /// </summary>
    public class ActiveMqTransportConfigurer : ConfigureTransport<NServiceBus.ActiveMQ>
    {
        protected override void InternalConfigure(Configure config, string brokerUri)
        {
            config.Configurer.ConfigureComponent<ActiveMqMessageReceiver>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested)
                  .ConfigureProperty(p => p.ConsumerName, NServiceBus.Configure.EndpointName);

            config.Configurer.ConfigureComponent<ActiveMqMessageSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqMessagePublisher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MessageProducer>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMQMessageDefer>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<ActiveMqSubscriptionStorage>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<SubscriptionManager>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<ActiveMqMessageMapper>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent(() => new ActiveMqMessageDecoderPipeline(), DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent(() => new ActiveMqMessageEncoderPipeline(), DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MessageTypeInterpreter>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<TopicEvaluator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<DestinationEvaluator>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<ActiveMqQueueCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqMessageDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<NotifyMessageReceivedFactory>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqPurger>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<SessionFactory>(DependencyLifecycle.SingleInstance);

            var factory = new NetTxConnectionFactory(brokerUri)
            {
                AcknowledgementMode =
                    Endpoint.IsVolatile ? AcknowledgementMode.AutoAcknowledge : AcknowledgementMode.Transactional,
            };

            config.Configurer.ConfigureComponent<INetTxConnectionFactory>(
                () => factory, DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent(
                () => (INetTxConnection)factory.CreateConnection(), DependencyLifecycle.SingleInstance);

            EndpointInputQueueCreator.Enabled = true;
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "activemq:tcp://localhost:61616"; }
        }
    }
}