namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    class TransportComponent
    {
        protected TransportComponent(TransportInfrastructure transportInfrastructure)
        {
            this.transportInfrastructure = transportInfrastructure;
        }

        public static TransportComponent Initialize(Configuration configuration, SettingsHolder settings)
        {
            var transportDefinition = configuration.TransportDefinition;
            var connectionString = configuration.TransportConnectionString.GetConnectionStringOrRaiseError(transportDefinition);

            var transportInfrastructure = transportDefinition.Initialize(settings, connectionString);

            settings.Set(transportInfrastructure);

            var transportType = transportDefinition.GetType();

            settings.AddStartupDiagnosticsSection("Transport", new
            {
                Type = transportType.FullName,
                Version = FileVersionRetriever.GetFileVersion(transportType)
            });

            return new TransportComponent(transportInfrastructure);
        }

        public EndpointInstance BindToLocalEndpoint(EndpointInstance endpointInstance)
        {
            return transportInfrastructure.BindToLocalEndpoint(endpointInstance);
        }

        public TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return transportInfrastructure.ConfigureReceiveInfrastructure();
        }

        public string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return transportInfrastructure.ToTransportAddress(logicalAddress);
        }

        public Task Start()
        {
            return transportInfrastructure.Start();
        }

        public Task Stop()
        {
            return transportInfrastructure.Stop();
        }

        readonly TransportInfrastructure transportInfrastructure;

        public class Configuration
        {
            public Configuration(SettingsHolder settings)
            {
                this.settings = settings;

                settings.SetDefault(TransportConnectionString.Default);
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

            readonly SettingsHolder settings;
        }
    }
}