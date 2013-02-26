namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using NServiceBus.Unicast.Queuing.Installers;
    using RabbitMQ = NServiceBus.RabbitMQ;

    public class RabbitMqTransportConfigurer : ConfigureTransport<RabbitMQ>
    {
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "host=localhost"; }
        }

        protected override void InternalConfigure(Configure config, string connectionString)
        {
            var parser = new RabbitMqConnectionStringParser(connectionString);

            if (!NServiceBus.Configure.Instance.Configurer.HasComponent<IManageRabbitMqConnections>())
            {
              
                var connectionManager = new RabbitMqConnectionManager(parser.BuildConnectionFactory(),parser.BuildConnectionRetrySettings());
                
                config.Configurer.RegisterSingleton<IManageRabbitMqConnections>(connectionManager);                
            }

            config.Configurer.ConfigureComponent<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested)
                 .ConfigureProperty(p => p.PrefetchCount, parser.GetPrefetchCount());

            config.Configurer.ConfigureComponent<RabbitMqUnitOfWork>(DependencyLifecycle.InstancePerCall);
            
            config.Configurer.ConfigureComponent<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<RabbitMqMessagePublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ExchangeName, RabbitMqConventions.ExchangeNameConvention);

            config.Configurer.ConfigureComponent<RabbitMqSubscriptionManager>(DependencyLifecycle.SingleInstance)
             .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue)
             .ConfigureProperty(p => p.ExchangeName, RabbitMqConventions.ExchangeNameConvention);

            config.Configurer.ConfigureComponent<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);

            EndpointInputQueueCreator.Enabled = true;
        }

      

    }

    public class RabbitMqConventions
    {
        /// <summary>
        /// Sets the convention for the name of the exchange(s) used for publish and subscribe
        /// </summary>
        /// <param name="exchangeNameConvention"></param>
        public void ExchangeNameForPubSub(Func<Address,Type, string> exchangeNameConvention)
        {
            ExchangeNameConvention = exchangeNameConvention;
        }

        /// <summary>
        /// Name of the topic where events are published to
        /// </summary>
        public static Func<Address,Type, string> ExchangeNameConvention = (address,eventType) => "amq.topic";
    }
}