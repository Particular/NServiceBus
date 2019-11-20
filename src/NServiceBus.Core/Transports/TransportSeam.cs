namespace NServiceBus
{
    using System;
    using Settings;
    using Transport;

    class TransportSeam
    {
        public static TransportInfrastructure Create(Settings transportSettings)
        {
            var transportDefinition = transportSettings.TransportDefinition;
            var connectionString = transportSettings.TransportConnectionString.GetConnectionStringOrRaiseError(transportDefinition);

            var transportInfrastructure = transportDefinition.Initialize(transportSettings.settings, connectionString);

            //RegisterTransportInfrastructureForBackwardsCompatibility
            transportSettings.settings.Set(transportInfrastructure);

            return transportInfrastructure;
        }

        public class Settings
        {
            public Settings(SettingsHolder settings)
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
                set => settings.Set(value);
            }

            public TransportConnectionString TransportConnectionString
            {
                get => settings.Get<TransportConnectionString>();
                set => settings.Set(value);
            }

            public QueueBindings QueueBindings => settings.Get<QueueBindings>();

            internal readonly SettingsHolder settings;
        }
    }
}