namespace NServiceBus.RabbitMq.Config
{
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
            var builder = new RabbitMqConnectionStringBuilder(connectionString);

            var connectionFactory = builder.BuildConnectionFactory();

            config.Configurer.ConfigureComponent(connectionFactory.CreateConnection, DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);

            config.Configurer.ConfigureComponent<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<RabbitMqMessagePublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue);

            config.Configurer.ConfigureComponent<RabbitMqSubscriptionManager>(DependencyLifecycle.InstancePerCall)
             .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue);

            config.Configurer.ConfigureComponent<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);

            EndpointInputQueueCreator.Enabled = true;
        }
    }
}