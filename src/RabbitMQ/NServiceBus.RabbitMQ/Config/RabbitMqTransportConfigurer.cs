namespace NServiceBus.Transports.RabbitMQ.Config
{
    using System;
    using EasyNetQ;
    using EasyNetQ.ConnectionString;
    using NServiceBus.Unicast.Queuing.Installers;
    using Settings;
    using Unicast.Subscriptions;
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
                var connectionStringParser = new ConnectionStringParser();
                var connectionConfiguration = connectionStringParser.Parse(connectionString);
                connectionConfiguration.OverrideClientProperties();
                config.Configurer.ConfigureComponent(() => connectionConfiguration, DependencyLifecycle.SingleInstance);
                config.Configurer.ConfigureComponent<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>(x => 
                    new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>(), DependencyLifecycle.InstancePerCall);
                config.Configurer.ConfigureComponent<IConnectionFactory>(x =>
                    new ConnectionFactoryWrapper(
                        x.Build<IConnectionConfiguration>(),
                        x.Build<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>()), DependencyLifecycle.InstancePerCall);
                var connectionFactory = NServiceBus.Configure.Instance.Builder.Build < IConnectionFactory>();
                
                var connectionManager = new RabbitMqConnectionManager(connectionFactory, parser.BuildConnectionRetrySettings());
                config.Configurer.RegisterSingleton<IManageRabbitMqConnections>(connectionManager);
            }


            config.Configurer.ConfigureComponent<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested)
                 .ConfigureProperty(p => p.PrefetchCount, parser.GetPrefetchCount());

            config.Configurer.ConfigureComponent<RabbitMqUnitOfWork>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.UsePublisherConfirms, parser.UsePublisherConfirms())
                  .ConfigureProperty(p => p.MaxWaitTimeForConfirms, parser.GetMaxWaitTimeForConfirms());


            config.Configurer.ConfigureComponent<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<RabbitMqMessagePublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.ExchangeName, SettingsHolder.Get<Func<Address, Type, string>>("Conventions.RabbitMq.ExchangeNameForPubSub"));

            config.Configurer.ConfigureComponent<RabbitMqSubscriptionManager>(DependencyLifecycle.SingleInstance)
             .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue)
             .ConfigureProperty(p => p.ExchangeName, SettingsHolder.Get<Func<Address, Type, string>>("Conventions.RabbitMq.ExchangeNameForPubSub"));

            config.Configurer.ConfigureComponent<RabbitMqRoutingKeyBuilder>(DependencyLifecycle.SingleInstance)
            .ConfigureProperty(p => p.GenerateRoutingKey, SettingsHolder.Get<Func<Type, string>>("Conventions.RabbitMq.RoutingKeyForEvent"));



            config.Configurer.ConfigureComponent<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<NoConfigRequiredAutoSubscriptionStrategy>(DependencyLifecycle.InstancePerCall);

            EndpointInputQueueCreator.Enabled = true;
        }
    }
}