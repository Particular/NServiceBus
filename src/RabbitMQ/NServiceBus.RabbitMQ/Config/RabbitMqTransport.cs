namespace NServiceBus.Features
{
    using Config;
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

            var parser = new RabbitMqConnectionStringParser(connectionString);
            
            if (!NServiceBus.Configure.Instance.Configurer.HasComponent<IManageRabbitMqConnections>())
            {

                var connectionManager = new RabbitMqConnectionManager(parser.BuildConnectionFactory(), parser.BuildConnectionRetrySettings());

                NServiceBus.Configure.Instance.Configurer.RegisterSingleton<IManageRabbitMqConnections>(connectionManager);
            }

            NServiceBus.Configure.Component<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested)
                 .ConfigureProperty(p => p.PrefetchCount, parser.GetPrefetchCount());

            NServiceBus.Configure.Component<RabbitMqUnitOfWork>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.UsePublisherConfirms, parser.UsePublisherConfirms())
                  .ConfigureProperty(p => p.MaxWaitTimeForConfirms, parser.GetMaxWaitTimeForConfirms());


            NServiceBus.Configure.Component<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<RabbitMqMessagePublisher>(DependencyLifecycle.InstancePerCall);

            NServiceBus.Configure.Component<RabbitMqSubscriptionManager>(DependencyLifecycle.SingleInstance)
             .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue);

            NServiceBus.Configure.Component<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);

            InfrastructureServices.Enable<IRoutingTopology>();

            EndpointInputQueueCreator.Enabled = true;
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