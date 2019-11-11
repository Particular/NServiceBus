namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Settings;
    using Transport;

    class TransportComponent
    {
        protected TransportComponent(TransportInfrastructure transportInfrastructure, QueueBindings queueBindings)
        {
            this.transportInfrastructure = transportInfrastructure;
            QueueBindings = queueBindings;
        }

        public static TransportComponent Initialize(Configuration configuration, SettingsHolder settings)
        {
            var transportDefinition = configuration.TransportDefinition;
            var connectionString = configuration.TransportConnectionString.GetConnectionStringOrRaiseError(transportDefinition);

            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);

            //for backwards compatibility
            settings.Set(transportInfrastructure);

            var transportType = transportDefinition.GetType();

            settings.AddStartupDiagnosticsSection("Transport", new
            {
                Type = transportType.FullName,
                Version = FileVersionRetriever.GetFileVersion(transportType)
            });

            return new TransportComponent(transportInfrastructure, configuration.QueueBindings);
        }

        public EndpointInstance BindToLocalEndpoint(EndpointInstance endpointInstance)
        {
            return transportInfrastructure.BindToLocalEndpoint(endpointInstance);
        }

        public Func<IPushMessages> GetMessagePumpFactory()
        {
            if (transportReceiveInfrastructure == null)
            {
                transportReceiveInfrastructure = transportInfrastructure.ConfigureReceiveInfrastructure();
            }

            return transportReceiveInfrastructure.MessagePumpFactory;
        }

        public string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return transportInfrastructure.ToTransportAddress(logicalAddress);
        }

        public Task CreateQueuesIfNecessary(string username)
        {
            if (transportReceiveInfrastructure == null)
            {
                return TaskEx.CompletedTask;
            }

            var queueCreator = transportReceiveInfrastructure.QueueCreatorFactory();

            return queueCreator.CreateQueueIfNecessary(QueueBindings, username);
        }

        public QueueBindings QueueBindings { get; }

        public IPushMessages BuildMessagePump()
        {
            return transportReceiveInfrastructure.MessagePumpFactory();
        }

        public async Task Start()
        {
            if (transportReceiveInfrastructure != null)
            {
                var result = await transportReceiveInfrastructure.PreStartupCheck().ConfigureAwait(false);

                if (!result.Succeeded)
                {
                    throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
                }
            }

            await transportInfrastructure.Start().ConfigureAwait(false);
        }

        public Task Stop()
        {
            return transportInfrastructure.Stop();
        }

        TransportInfrastructure transportInfrastructure;
        TransportReceiveInfrastructure transportReceiveInfrastructure;

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
            {
                this.settings = settings;

                settings.SetDefault(TransportConnectionString.Default);
                settings.Set(new QueueBindings());
            }

            public TransportDefinition TransportDefinition
            {
                get
                {
                    if (!settings.HasExplicitValue<TransportDefinition>())
                    {
                        throw new Exception("A transport has not been configured. Use 'EndpointConfiguration.UseTransport()' to specify a transport.");
                    }

                    return settings.Get<TransportDefinition>();
                }
                set
                {
                    settings.Set(value);
                }
            }

            public TransportConnectionString TransportConnectionString
            {
                get
                {
                    return settings.Get<TransportConnectionString>();
                }
                set
                {
                    settings.Set(value);
                }
            }

            public QueueBindings QueueBindings
            {
                get
                {
                    return settings.Get<QueueBindings>();
                }
            }

            readonly SettingsHolder settings;
        }
    }
}
