namespace NServiceBus.Features
{
    using System;
    using Settings;
    using Transports;
    using Transports.SQLServer;
    using Unicast.Queuing.Installers;

    /// <summary>
    /// Configures NServiceBus to use SqlServer as the default transport
    /// </summary>
    public class SqlServerTransport : ConfigureTransport<SqlServer>
    {
        protected override string ExampleConnectionStringForErrorMessage
        {
            get { return @"Data Source=.\SQLEXPRESS;Initial Catalog=nservicebus;Integrated Security=True"; }
        }

        protected override void InternalConfigure(Configure config)
        {
            
            Enable<SqlServerTransport>();
            Enable<MessageDrivenSubscriptions>();
        }

        public override void Initialize()
        {
            //Until we refactor the whole address system
            CustomizeAddress();
            
            var connectionString = SettingsHolder.Get<string>("NServiceBus.Transport.ConnectionString");

            if (String.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Sql Transport connection string cannot be empty or null.");
            }

            NServiceBus.Configure.Component<SqlServerQueueCreator>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ConnectionString, connectionString);

            NServiceBus.Configure.Component<SqlServerMessageSender>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ConnectionString, connectionString);

            NServiceBus.Configure.Component<SqlServerPollingDequeueStrategy>(DependencyLifecycle.InstancePerCall)
                  .ConfigureProperty(p => p.ConnectionString, connectionString)
                  .ConfigureProperty(p => p.PurgeOnStartup, ConfigurePurging.PurgeRequested);

            EndpointInputQueueCreator.Enabled = true;
        }

        static void CustomizeAddress()
        {
            Address.IgnoreMachineName();

            if (!SettingsHolder.GetOrDefault<bool>("ScaleOut.UseSingleBrokerQueue"))
            {
                Address.InitializeLocalAddress(Address.Local.Queue + "." + Address.Local.Machine);
            }
         
        }
    }
}