namespace NServiceBus
{
    using System.Configuration;
    using RabbitMQ;
    using RabbitMQ.Config;
    using Unicast.Queuing.Installers;

    public static class ConfigureRabbitMqTransport
    {
       
         /// <summary>
         /// Configures Rabbit as the transport. Settings are read from the configuration
         /// </summary>
         /// <param name="config"></param>
         /// <returns></returns>
         public static Configure RabbitMqTransport(this Configure config)
         {
             var connectionString = ConfigurationManager.ConnectionStrings["NServiceBus.RabbitMQ"].ConnectionString;

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

            config.Configurer.ConfigureComponent(connectionFactory.CreateConnection, DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);


            EndpointInputQueueCreator.Enabled = true;

            return config;
        }
 
    }
}