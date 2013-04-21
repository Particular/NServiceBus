namespace NServiceBus.Features
{
    using Config;
    using EasyNetQ;
    using Settings;
    using Transports;
    using Transports.RabbitMQ;
    using Transports.RabbitMQ.Config;
    using Transports.RabbitMQ.Routing;
    using Unicast.Queuing.Installers;
    using Unicast.Subscriptions;

    public class RabbitMqTransport : ConfigureTransport<RabbitMQ>, IFeature
    {
        public void Initialize()
        {
            var connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");

            var connectionStringParser = new ConnectionStringParser();
            var connectionConfiguration = connectionStringParser.Parse(connectionString);

            if (!NServiceBus.Configure.Instance.Configurer.HasComponent<IManageRabbitMqConnections>())
            {
                ConfigureDefaultRabbitMqConnectionManager(connectionConfiguration);
            }

            NServiceBus.Configure.Component<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested)
                 .ConfigureProperty(p => p.PrefetchCount, connectionConfiguration.PrefetchCount);

            NServiceBus.Configure.Component<RabbitMqUnitOfWork>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.UsePublisherConfirms, connectionConfiguration.UsePublisherConfirms)
                  .ConfigureProperty(p => p.MaxWaitTimeForConfirms, connectionConfiguration.MaxWaitTimeForConfirms);


            NServiceBus.Configure.Component<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<RabbitMqMessagePublisher>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<RabbitMqSubscriptionManager>(DependencyLifecycle.SingleInstance)
             .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue);

            NServiceBus.Configure.Component<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);

            InfrastructureServices.Enable<IRoutingTopology>();

            EndpointInputQueueCreator.Enabled = true;
        }

        static void ConfigureDefaultRabbitMqConnectionManager(IConnectionConfiguration connectionConfiguration) {
            var config = NServiceBus.Configure.Instance.Configurer;
            config.ConfigureComponent(() => connectionConfiguration, DependencyLifecycle.SingleInstance);
            config.ConfigureComponent<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>(x =>
                new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>(), DependencyLifecycle.InstancePerCall);
            config.ConfigureComponent<IConnectionFactory>(x =>
                new ConnectionFactoryWrapper(
                x.Build<IConnectionConfiguration>(),
                x.Build<IClusterHostSelectionStrategy<ConnectionFactoryInfo>>()), DependencyLifecycle.InstancePerCall);
            var connectionFactory = NServiceBus.Configure.Instance.Builder.Build<IConnectionFactory>();
            var connectionManager = new RabbitMqConnectionManager(connectionFactory, connectionConfiguration);
            config.RegisterSingleton<IManageRabbitMqConnections>(connectionManager);
        }

        protected override void InternalConfigure(Configure config, string connectionString)
        {
            Feature.Enable<RabbitMqTransport>();
            InfrastructureServices.RegisterServiceFor<IAutoSubscriptionStrategy>(typeof(NoConfigRequiredAutoSubscriptionStrategy), DependencyLifecycle.InstancePerCall);
        }

        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return "host=localhost"; }
        }
    }
}