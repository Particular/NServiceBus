namespace NServiceBus.Transports.SQLServer.Transport.Config
{
    using System;
    using NServiceBus.Config;
    using NServiceBus.Unicast.Queuing.Installers;

    /// <summary>
    /// Configures NServiceBus to use SqlServer as the default transport
    /// </summary>
    public class SqlServerTransportConfigurer : ConfigureTransport<SqlServer>
    {
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True"; }
        }

        protected override void InternalConfigure(Configure config, string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Sql Transport connection string cannot be empty or null.");
            }

            config.Configurer.ConfigureComponent<SqlServerQueueCreator>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ConnectionString, connectionString);

            config.Configurer.ConfigureComponent<SqlServerMessageSender>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ConnectionString, connectionString);

            config.Configurer.ConfigureComponent<SqlServerPollingDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ConnectionString, connectionString)
                  .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested); ;

            EndpointInputQueueCreator.Enabled = true;
        }
    }
}