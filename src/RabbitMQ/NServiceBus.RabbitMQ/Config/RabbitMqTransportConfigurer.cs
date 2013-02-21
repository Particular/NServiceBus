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
                var connectionManager = new DefaultRabbitMqConnectionManager(connectionFactory);
                config.Configurer.RegisterSingleton<IManageRabbitMqConnections>(connectionManager);                
            }

            config.Configurer.ConfigureComponent<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);

            config.Configurer.ConfigureComponent<RabbitMqUnitOfWork>(DependencyLifecycle.InstancePerCall);
            
            config.Configurer.ConfigureComponent<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<RabbitMqMessagePublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ExchangeName, ExchangeNameConvention);

            config.Configurer.ConfigureComponent<RabbitMqSubscriptionManager>(DependencyLifecycle.SingleInstance)
             .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue)
             .ConfigureProperty(p => p.ExchangeName, ExchangeNameConvention);

            config.Configurer.ConfigureComponent<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ExchangeName, ExchangeNameConvention); ;

            EndpointInputQueueCreator.Enabled = true;
        }

        /// <summary>
        /// Name of the topic where events are published to
        /// </summary>
        public static Func<Address, string> ExchangeNameConvention = address => "amq.topic";

    }
}