namespace NServiceBus
{
    using System.Configuration;
    using RabbitMQ;
    using RabbitMQ.Config;
    using Unicast.Queuing.Installers;
    using global::RabbitMQ.Client;

    public static class ConfigureRabbitMqTransport
    {

        /// <summary>
        /// Configures Rabbit as the transport. Settings are read from the configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure RabbitMqTransport(this Configure config)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["NServiceBus/Transports/RabbitMQ"].ConnectionString;

            return RabbitMqTransport(config, connectionString);
        }
        /// <summary>
        /// Configures Rabbit as the transport. Settings are read from the configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configure RabbitMqTransport(this Configure config, string connectionString)
        {
            var builder = new RabbitMqConnectionStringBuilder(connectionString);

            var connectionFactory = builder.BuildConnectionFactory();

            return RabbitMqTransport(config, connectionFactory);
        }

        /// <summary>
        /// Configures the transport and connecting using the given connection factory
        /// </summary>
        /// <param name="config"></param>
        /// <param name="connectionFactory"></param>
        /// <returns></returns>
        public static Configure RabbitMqTransport(this Configure config, ConnectionFactory connectionFactory)
        {
            config.Configurer.ConfigureComponent(connectionFactory.CreateConnection, DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested); 
            config.Configurer.ConfigureComponent<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);


            EndpointInputQueueCreator.Enabled = true;

            return config;
        }

    }
}