namespace NServiceBus.Transport.ActiveMQ.Config
{
    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using NServiceBus.Config;
    using NServiceBus.Transport.ActiveMQ.Receivers;
    using NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes;
    using NServiceBus.Transport.ActiveMQ.SessionFactories;

    using Unicast.Queuing.Installers;
    using MessageProducer = NServiceBus.Transport.ActiveMQ.MessageProducer;

    /// <summary>
    /// Default configuration for ActiveMQ
    /// </summary>
    public class ActiveMqTransportConfigurer : ConfigureTransport<NServiceBus.ActiveMQ>
    {
        protected override void InternalConfigure(Configure config, string brokerUri)
        {
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
            config.Configurer.ConfigureComponent<ActiveMqMessageReceiver>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MessageProcessor>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);
            config.Configurer.ConfigureComponent<MessageCounter>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<NotifyMessageReceivedFactory>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ConsumerName, NServiceBus.Configure.EndpointName);
            config.Configurer.ConfigureComponent<ActiveMqPurger>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<TransactionScopeFactory>(DependencyLifecycle.SingleInstance);

            if (!NServiceBus.Configure.Transactions.Enabled)
            {
                RegisterNoneTransactionSessionFactory(config, brokerUri);
            }
            else
            {
                if (NServiceBus.Configure.Transactions.Advanced().SuppressDistributedTransactions)
                {
                    RegisterActiveMQManagedTransactionSessionFactory(config, brokerUri);
                }
                else
                {
                    RegisterDTCManagedTransactionSessionFactory(config, brokerUri);
                }
            }

            EndpointInputQueueCreator.Enabled = true;
        }

        private static void RegisterNoneTransactionSessionFactory(Configure config, string brokerUri)
        {
            var connectionFactory = new ConnectionFactory(brokerUri)
                {
                    AcknowledgementMode = AcknowledgementMode.IndividualAcknowledge, 
                    AsyncSend = true
                };
            var sessionFactory = new PooledSessionFactory(connectionFactory);

            config.Configurer.ConfigureComponent(() => sessionFactory, DependencyLifecycle.SingleInstance);
        }

        private static void RegisterActiveMQManagedTransactionSessionFactory(Configure config, string brokerUri)
        {
            var connectionFactory = new ConnectionFactory(brokerUri)
                {
                    AcknowledgementMode = AcknowledgementMode.Transactional
                };
            var pooledSessionFactory = new PooledSessionFactory(connectionFactory);
            var sessionFactory = new ActiveMqTransactionSessionFactory(pooledSessionFactory);

            config.Configurer.ConfigureComponent(() => sessionFactory, DependencyLifecycle.SingleInstance);
        }

        private static void RegisterDTCManagedTransactionSessionFactory(Configure config, string brokerUri)
        {
            var connectionFactory = new NetTxConnectionFactory(brokerUri)
            {
                AcknowledgementMode = AcknowledgementMode.Transactional
            };
            var pooledSessionFactory = new PooledSessionFactory(connectionFactory);
            var sessionFactory = new DTCTransactionSessionFactory(pooledSessionFactory);

            config.Configurer.ConfigureComponent(() => sessionFactory, DependencyLifecycle.SingleInstance);
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "activemq:tcp://localhost:61616"; }
        }
    }
}