using System.Threading.Tasks;

namespace NServiceBus
{
    using System;
    using Settings;
    using Transport;

    class TransportSeam
    {
        public TransportDefinition TransportDefinition { get;  }

        protected TransportSeam(TransportDefinition transportDefinition, QueueBindings queueBindings)
        {
            TransportDefinition = transportDefinition;
            QueueBindings = queueBindings;
        }

        public static TransportSeam Create(Settings transportSettings, HostingComponent.Configuration hostingConfiguration)
        {
            var transportDefinition = transportSettings.TransportDefinition;

            transportSettings.settings.Set(transportDefinition);

            return new TransportSeam(transportDefinition, transportSettings.QueueBindings);
        }

        public QueueBindings QueueBindings { get; }

        public class Settings
        {
            public Settings(SettingsHolder settings)
            {
                this.settings = settings;

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

            public QueueBindings QueueBindings => settings.Get<QueueBindings>();

            internal readonly SettingsHolder settings;
        }
    }
}