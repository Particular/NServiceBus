namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Apache.NMS;
    using Apache.NMS.ActiveMQ;
    using Apache.NMS.Policies;
    using Config;
    using Transports;
    using Transports.ActiveMQ;
    using Transports.ActiveMQ.Receivers;
    using Transports.ActiveMQ.Receivers.TransactionsScopes;
    using Transports.ActiveMQ.SessionFactories;
    using Unicast.Queuing.Installers;
    using Settings;
    using MessageProducer = Transports.ActiveMQ.MessageProducer;

    /// <summary>
    /// Default configuration for ActiveMQ
    /// </summary>
    public class ActiveMqTransport : ConfigureTransport<ActiveMQ>
    {
        public override void Initialize()
        {
            if (!SettingsHolder.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
            {
                Address.InitializeLocalAddress(Address.Local.Queue + "." + Address.Local.Machine);
            }

            var connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");

            var connectionConfiguration = Parse(connectionString);

            NServiceBus.Configure.Component<ActiveMqMessageSender>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<ActiveMqMessagePublisher>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<MessageProducer>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<SubscriptionManager>(DependencyLifecycle.SingleInstance);

            NServiceBus.Configure.Component<ActiveMqMessageMapper>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component(() => new ActiveMqMessageDecoderPipeline(), DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component(() => new ActiveMqMessageEncoderPipeline(), DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<MessageTypeInterpreter>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<TopicEvaluator>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<DestinationEvaluator>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<ActiveMqQueueCreator>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<ActiveMqMessageDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            NServiceBus.Configure.Component<ActiveMqMessageReceiver>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<NotifyMessageReceivedFactory>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ConsumerName, NServiceBus.Configure.EndpointName);
            NServiceBus.Configure.Component<ActiveMqPurger>(DependencyLifecycle.SingleInstance);
            NServiceBus.Configure.Component<TransactionScopeFactory>(DependencyLifecycle.SingleInstance);

            NServiceBus.Configure.Component<MessageProcessor>(DependencyLifecycle.InstancePerCall)
              .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);

            if (!SettingsHolder.Get<bool>("Transactions.Enabled"))
            {
                NServiceBus.Configure.Component<ActiveMQMessageDefer>(DependencyLifecycle.InstancePerCall);
                NServiceBus.Configure.Component<ActiveMqSchedulerManagement>(DependencyLifecycle.SingleInstance)
                      .ConfigureProperty(p => p.Disabled, false);
                NServiceBus.Configure.Component<ActiveMqSchedulerManagementJobProcessor>(DependencyLifecycle.SingleInstance);
                NServiceBus.Configure.Component<ActiveMqSchedulerManagementCommands>(DependencyLifecycle.SingleInstance);

                RegisterNoneTransactionSessionFactory(connectionConfiguration[UriKey]);
            }
            else
            {
                var transportConfig = NServiceBus.Configure.GetConfigSection<TransportConfig>();

                var maxRetries = transportConfig == null ? 6 : transportConfig.MaxRetries + 1;

                if (SettingsHolder.Get<bool>("Transactions.SuppressDistributedTransactions"))
                {
                    RegisterActiveMQManagedTransactionSessionFactory(maxRetries, connectionConfiguration[UriKey]);
                }
                else
                {
                    RegisterDTCManagedTransactionSessionFactory(maxRetries, connectionConfiguration);
                }
            }
        }

        protected override void InternalConfigure(Configure config)
        {
            Enable<ActiveMqTransport>();
        }
        
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "ServerUrl=activemq:tcp://localhost:61616; ResourceManagerId=2f2c3321-f251-4975-802d-11fc9d9e5e37"; }
        }
        
        Dictionary<string, string> Parse(string brokerUri)
        {
            var parts = brokerUri.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.ToDictionary(
                p => p.Split('=')[0].Trim(),
                p => p.Substring(p.IndexOf("=", StringComparison.OrdinalIgnoreCase) + 1).Trim(),
                StringComparer.OrdinalIgnoreCase);
        }

        static void RegisterNoneTransactionSessionFactory(string brokerUri)
        {
            var connectionFactory = new ConnectionFactory(brokerUri)
                {
                    AcknowledgementMode = AcknowledgementMode.IndividualAcknowledge, 
                    AsyncSend = true
                };
            var sessionFactory = new PooledSessionFactory(connectionFactory);

            NServiceBus.Configure.Component(() => sessionFactory, DependencyLifecycle.SingleInstance);
        }

        static void RegisterActiveMQManagedTransactionSessionFactory(int maxRetries, string brokerUri)
        {
            var connectionFactory = new ConnectionFactory(brokerUri)
                {
                    AcknowledgementMode = AcknowledgementMode.Transactional,
                    RedeliveryPolicy = new RedeliveryPolicy { MaximumRedeliveries = maxRetries, BackOffMultiplier = 0, UseExponentialBackOff = false }
                };
            var pooledSessionFactory = new PooledSessionFactory(connectionFactory);
            var sessionFactory = new ActiveMqTransactionSessionFactory(pooledSessionFactory);

            NServiceBus.Configure.Component(() => sessionFactory, DependencyLifecycle.SingleInstance);
        }

        static void RegisterDTCManagedTransactionSessionFactory(int maxRetries, Dictionary<string, string> connectionConfiguration)
        {
            NetTxConnection.ConfiguredResourceManagerId = connectionConfiguration.ContainsKey(ResourceManagerIdKey) 
                ? new Guid(connectionConfiguration[ResourceManagerIdKey])
                : DefaultResourceManagerId;
            var connectionFactory = new NetTxConnectionFactory(connectionConfiguration[UriKey])
            {
                AcknowledgementMode = AcknowledgementMode.Transactional,
                RedeliveryPolicy = new RedeliveryPolicy { MaximumRedeliveries = maxRetries, BackOffMultiplier = 0, UseExponentialBackOff = false }
            };
            var pooledSessionFactory = new PooledSessionFactory(connectionFactory);
            var sessionFactory = new DTCTransactionSessionFactory(pooledSessionFactory);

            NServiceBus.Configure.Component(() => sessionFactory, DependencyLifecycle.SingleInstance);
        }


        static Guid DefaultResourceManagerId
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
                var inputBytes = Encoding.Default.GetBytes(input);
                var hashBytes = provider.ComputeHash(inputBytes);
                //generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }


        const string UriKey = "ServerUrl";

        const string ResourceManagerIdKey = "ResourceManagerId";

    }
}