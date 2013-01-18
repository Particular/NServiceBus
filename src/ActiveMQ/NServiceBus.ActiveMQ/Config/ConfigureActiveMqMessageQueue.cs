namespace NServiceBus
{
    using System;
    using System.Configuration;

    using Apache.NMS;
    using Apache.NMS.ActiveMQ;

    using NServiceBus.Transport.ActiveMQ;
    using NServiceBus.Unicast.Queuing.Installers;
    using Unicast.Transport;
    using MessageProducer = NServiceBus.Transport.ActiveMQ.MessageProducer;

    public static class ConfigureActiveMqMessageQueue
    {
        private const string Message =
            @"
To run NServiceBus with ActiveMQ Transport you need to specify the database connectionstring.
Here is an example of what is required:
  
  <connectionStrings>
    <add name=""NServiceBus/Transport"" connectionString=""activemq:tcp://localhost:61616"" />
  </connectionStrings>";

        /// <summary>
        /// Configures ActiveMQ as the transport.
        /// </summary>
        /// <remarks>
        /// Reads configuration settings from <a href="http://msdn.microsoft.com/en-us/library/bf7sd233">&lt;connectionStrings&gt; config section</a>.
        /// </remarks>
        /// <example>
        /// An example that shows the configuration:
        /// <code lang="XML" escaped="true">
        ///  <connectionStrings>
        ///    <!-- Default connection string name -->
        ///    <add name="NServiceBus/Transport" connectionString="activemq:tcp://localhost:61616" />
        ///  </connectionStrings>
        /// </code>
        /// </example>
        /// <param name="config">The configuration object.</param>
        /// <returns>The configuration object.</returns>
        public static Configure ActiveMQTransport(this Configure config)
        {
            string defaultConnectionString = TransportConnectionString.GetConnectionStringOrNull();

            if (defaultConnectionString == null)
            {
                string errorMsg =
                    @"No default connection string found in your config file ({0}) for the ActiveMQ Transport.
{1}";
                throw new InvalidOperationException(String.Format(errorMsg, GetConfigFileIfExists(), Message));
            }

            return config.InternalActiveMQTransport(defaultConnectionString);
        }

        /// <summary>
        /// Configures ActiveMQ as the transport.
        /// </summary>
        /// <param name="configure">The configuration object.</param>
        /// <param name="connectionStringName">The connectionstring name to use to retrieve the connectionstring from.</param>
        /// <returns>The configuration object.</returns>
        public static Configure ActiveMQTransport(this Configure configure, string connectionStringName)
        {
            string defaultConnectionString = GetConnectionStringOrNull(connectionStringName);

            if (defaultConnectionString == null)
            {
                string errorMsg =
                    @"The connection string named ({0}) was not found in your config file ({1}).";
                throw new InvalidOperationException(String.Format(errorMsg, connectionStringName,
                                                                  GetConfigFileIfExists()));
            }

            return configure.InternalActiveMQTransport(defaultConnectionString);
        }

        /// <summary>
        /// Configures ActiveMQ as the transport.
        /// </summary>
        /// <param name="configure">The configuration object.</param>
        /// <param name="definesConnectionString">Specifies a callback to call to retrieve the connectionstring to use</param>
        /// <returns>The configuration object.</returns>
        public static Configure ActiveMQTransport(this Configure configure, Func<string> definesConnectionString)
        {
            return configure.InternalActiveMQTransport(definesConnectionString());
        }

        /// <summary>
        /// Use MSMQ for your queuing infrastructure.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="brokerUri">Address of the ActiveMQ broker to connect to</param>
        /// <returns></returns>
        static Configure InternalActiveMQTransport(this Configure config, string brokerUri)
        {
            config.Configurer.ConfigureComponent<ActiveMqMessageReceiver>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested)
                  .ConfigureProperty(p => p.ConsumerName, Configure.EndpointName);

            config.Configurer.ConfigureComponent<ActiveMqMessageSender>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqMessagePublisher>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MessageProducer>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<ActiveMqSubscriptionStorage>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<SubscriptionManager>(DependencyLifecycle.SingleInstance);

            config.Configurer.ConfigureComponent<ActiveMqMessageMapper>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent(() => new ActiveMqMessageDecoderPipeline(), DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent(() => new ActiveMqMessageEncoderPipeline(), DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<MessageTypeInterpreter>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<TopicEvaluator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<DestinationEvaluator>(DependencyLifecycle.InstancePerCall);

            config.Configurer.ConfigureComponent<ActiveMqQueueCreator>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqMessageDequeueStrategy>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<NotifyMessageReceivedFactory>(DependencyLifecycle.InstancePerCall);
            config.Configurer.ConfigureComponent<ActiveMqPurger>(DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent<SessionFactory>(DependencyLifecycle.SingleInstance);

            var factory = new NetTxConnectionFactory(brokerUri)
                {
                    AcknowledgementMode =
                        Endpoint.IsVolatile ? AcknowledgementMode.AutoAcknowledge : AcknowledgementMode.Transactional,
                };

            config.Configurer.ConfigureComponent<INetTxConnectionFactory>(
                () => factory, DependencyLifecycle.SingleInstance);
            config.Configurer.ConfigureComponent(
                () => (INetTxConnection)factory.CreateConnection(), DependencyLifecycle.SingleInstance);

            EndpointInputQueueCreator.Enabled = true;

            return config;
        }

        private static string GetConfigFileIfExists()
        {
            return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile ?? "App.config";
        }

        private static string GetConnectionStringOrNull(string name)
        {
            ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[name];

            if (connectionStringSettings == null)
            {
                return null;
            }

            return connectionStringSettings.ConnectionString;
        }
    }
}