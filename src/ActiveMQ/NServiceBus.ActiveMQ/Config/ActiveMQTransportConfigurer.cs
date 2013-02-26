namespace NServiceBus.Transports.ActiveMQ.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using NServiceBus.Config;
    using NServiceBus.Unicast.Queuing.Installers;
    using Receivers;
    using Receivers.TransactonsScopes;
    using SessionFactories;
    using MessageProducer = ActiveMQ.MessageProducer;

    /// <summary>
    /// Default configuration for ActiveMQ
    /// </summary>
    public class ActiveMqTransportConfigurer : ConfigureTransport<NServiceBus.ActiveMQ>
    {
        private const string UriKey = "ServerUrl";

        private const string ResourceManagerIdKey = "ResourceManagerId";

        protected override void InternalConfigure(Configure config, string brokerUri)
        {
            var connectionConfiguration = this.Parse(brokerUri);
            config.Configurer.ConfigureComponent<ActiveMqMessageSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqMessagePublisher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MessageProducer>(DependencyLifecycle.InstancePerCall);
            
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
                config.Configurer.ConfigureComponent<ActiveMQMessageDefer>(DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<ActiveMqSchedulerManagement>(DependencyLifecycle.SingleInstance)
                      .ConfigureProperty(p => p.Disabled, false);
                config.Configurer.ConfigureComponent<ActiveMqSchedulerManagementJobProcessor>(DependencyLifecycle.SingleInstance);
                config.Configurer.ConfigureComponent<ActiveMqSchedulerManagementCommands>(DependencyLifecycle.SingleInstance);

                RegisterNoneTransactionSessionFactory(config, connectionConfiguration[UriKey]);
            }
            else
            {
                if (NServiceBus.Configure.Transactions.Advanced().SuppressDistributedTransactions)
                {
                    RegisterActiveMQManagedTransactionSessionFactory(config, connectionConfiguration[UriKey]);
                }
                else
                {
                    RegisterDTCManagedTransactionSessionFactory(config, connectionConfiguration);
                }
            }

            EndpointInputQueueCreator.Enabled = true;
        }

        private Dictionary<string, string> Parse(string brokerUri)
        {
            var parts = brokerUri.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.ToDictionary(
                p => p.Split('=')[0].Trim(),
                p => p.Substring(p.IndexOf("=", StringComparison.OrdinalIgnoreCase) + 1).Trim(), StringComparer.OrdinalIgnoreCase);
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

        private static void RegisterDTCManagedTransactionSessionFactory(Configure config, Dictionary<string, string> connectionConfiguration)
        {
            NetTxConnection.ConfiguredResourceManagerId = connectionConfiguration.ContainsKey(ResourceManagerIdKey) 
                ? new Guid(connectionConfiguration[ResourceManagerIdKey])
                : DefaultResourceManagerId;
            var connectionFactory = new NetTxConnectionFactory(connectionConfiguration[UriKey])
            {
                AcknowledgementMode = AcknowledgementMode.Transactional
            };
            var pooledSessionFactory = new PooledSessionFactory(connectionFactory);
            var sessionFactory = new DTCTransactionSessionFactory(pooledSessionFactory);

            config.Configurer.ConfigureComponent(() => sessionFactory, DependencyLifecycle.SingleInstance);
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "ServerUrl=activemq:tcp://localhost:61616; ResourceManagerId=2f2c3321-f251-4975-802d-11fc9d9e5e37"; }
        }

        public static Guid DefaultResourceManagerId
        {
            get
            {
                var resourceManagerId = "ActiveMQ" + Address.Local + "-" + NServiceBus.Configure.DefineEndpointVersionRetriever();
                return DeterministicGuidBuilder(resourceManagerId);
            }
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            //use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                byte[] inputBytes = Encoding.Default.GetBytes(input);
                byte[] hashBytes = provider.ComputeHash(inputBytes);
                //generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}