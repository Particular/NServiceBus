namespace NServiceBus.RabbitMq.Config
{
    using System;
    using NServiceBus.Config;
    using Unicast.Queuing.Installers;

    public class RabbitMqTransportConfigurer : ConfigureTransport<RabbitMQ>
    {
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "host=localhost"; }
        }

        protected override void InternalConfigure(Configure config, string connectionString)
        {
            if (!NServiceBus.Configure.Instance.Configurer.HasComponent<IManageRabbitMqConnections>())
            {
                var builder = new RabbitMqConnectionStringBuilder(connectionString);

                var connectionFactory = builder.BuildConnectionFactory();
                var connectionManager = new RabbitMqConnectionManager(connectionFactory);
                config.Configurer.RegisterSingleton<IManageRabbitMqConnections>(connectionManager);                
            }

            config.Configurer.ConfigureComponent<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);

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