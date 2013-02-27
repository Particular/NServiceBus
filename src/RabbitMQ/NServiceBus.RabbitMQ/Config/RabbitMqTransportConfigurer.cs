namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using NServiceBus.Unicast.Queuing.Installers;
    using Settings;
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
                .ConfigureProperty(p => p.ExchangeName, SettingsHolder.Get<Func<Address, Type, string>>("Conventions.RabbitMq.ExchangeNameForPubSub"));

            config.Configurer.ConfigureComponent<RabbitMqSubscriptionManager>(DependencyLifecycle.SingleInstance)
             .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue)
             .ConfigureProperty(p => p.ExchangeName, SettingsHolder.Get<Func<Address, Type, string>>("Conventions.RabbitMq.ExchangeNameForPubSub"));

            config.Configurer.ConfigureComponent<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);

            EndpointInputQueueCreator.Enabled = true;
        }

      

    }
}