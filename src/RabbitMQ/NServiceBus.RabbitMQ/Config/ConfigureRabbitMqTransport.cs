namespace NServiceBus
{
    using System;
    using System.Configuration;
    using NServiceBus.Unicast.Queuing.Installers;
    using RabbitMq;
    using RabbitMq.Config;
    using Unicast.Transport;
    using global::RabbitMQ.Client;

    public static class ConfigureRabbitMqTransport
    {
        private const string Message =
            @"
To run NServiceBus with RabbitMQ Transport you need to specify the database connectionstring.
Here is an example of what is required:
  
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""host=localhost"" />
  </connectionStrings>";

        /// <summary>
        /// Configures RabbitMQ as the transport.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the configuration:
        /// <code lang="XML" escaped="true">
        ///  <connectionStrings>
        ///    <!-- Default connection string name -->
        ///    <add name="NServiceBus/Transport" connectionString="host=localhost" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RabbitMQTransport(this Configure config)
        {
            string defaultConnectionString = TransportConnectionString.GetConnectionStringOrNull();

            if (defaultConnectionString == null)
            {
                string errorMsg =
                    @"No default connection string found in your config file ({0}) for the RabbitMQ Transport.
{1}";
                throw new InvalidOperationException(String.Format(errorMsg, GetConfigFileIfExists(), Message));
            }

            return InternalRabbitMQTransport(config, defaultConnectionString);
        }

        /// <summary>
        /// Configures RabbitMQ as the transport.
        /// </summary>
        /// <param name="configure">The configuration object.</param>
        /// <param name="connectionStringName">The connectionstring name to use to retrieve the connectionstring from.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RabbitMQTransport(this Configure configure, string connectionStringName)
        {
            string defaultConnectionString = TransportConnectionString.GetConnectionStringOrNull();

            if (defaultConnectionString == null)
            {
                string errorMsg =
                    @"The connection string named ({0}) was not found in your config file ({1}).";
                throw new InvalidOperationException(String.Format(errorMsg, connectionStringName,
                                                                  GetConfigFileIfExists()));
            }

            return configure.InternalRabbitMQTransport(defaultConnectionString);
        }

        /// <summary>
        /// Configures RabbitMQ as the transport.
        /// </summary>
        /// <param name="configure">The configuration object.</param>
        /// <param name="definesConnectionString">Specifies a callback to call to retrieve the connectionstring to use</param>
        /// <returns>The configuration object.</returns>
        public static Configure RabbitMQTransport(this Configure configure, Func<string> definesConnectionString)
        {
            return configure.InternalRabbitMQTransport(definesConnectionString());
        }

        /// <summary>
        /// Configures RabbitMQ as the transport using the given connection factory.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        /// <param name="connectionFactory"><see cref="ConnectionFactory"/> to use to connect.</param>
        /// <returns>The configuration object.</returns>
        public static Configure RabbitMQTransport(this Configure config, ConnectionFactory connectionFactory)
        {
            config.Configurer.ConfigureComponent(connectionFactory.CreateConnection, DependencyLifecycle.SingleInstance);
            
            config.Configurer.ConfigureComponent<RabbitMqDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                 .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested); 
            
            config.Configurer.ConfigureComponent<RabbitMqMessageSender>(DependencyLifecycle.InstancePerCall);
            
            config.Configurer.ConfigureComponent<RabbitMqMessagePublisher>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(p=>p.EndpointQueueName,Address.Local.Queue);

            config.Configurer.ConfigureComponent<RabbitMqSubscriptionManager>(DependencyLifecycle.InstancePerCall)
             .ConfigureProperty(p => p.EndpointQueueName, Address.Local.Queue);

            config.Configurer.ConfigureComponent<RabbitMqQueueCreator>(DependencyLifecycle.InstancePerCall);
            
            EndpointInputQueueCreator.Enabled = true;

            return config;
        }

        static Configure InternalRabbitMQTransport(this Configure config, string connectionString)
        {
            var builder = new RabbitMqConnectionStringBuilder(connectionString);

            var connectionFactory = builder.BuildConnectionFactory();

            return RabbitMQTransport(config, connectionFactory);
        }

        private static string GetConfigFileIfExists()
        {
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile ?? "App.config";
        }
    }
}