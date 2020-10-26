using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NServiceBus
{
    using System;
    using Settings;
    using Transport;

    class TransportSeam
    {
        Transport.Settings transportSettings;
        ReceiveSettings[] receivers;

        public TransportDefinition TransportDefinition { get;  }

        protected TransportSeam(TransportDefinition transportDefinition, Transport.Settings transportSettings,
            QueueBindings queueBindings)
        {
            TransportDefinition = transportDefinition;
            QueueBindings = queueBindings;
            this.transportSettings = transportSettings;
        }

        //TODO can also be moved to Initialize
        public void Configure(ReceiveSettings[] receivers)
        {
            this.receivers = receivers;
        }

        public async Task<TransportInfrastructure> Initialize()
        {
            return await TransportDefinition.Initialize(transportSettings, receivers, QueueBindings.SendingAddresses.ToArray(), CancellationToken.None)
                .ConfigureAwait(false);
        }

        public static TransportSeam Create(Settings transportSeamSettings, HostingComponent.Configuration hostingConfiguration)
        {
            var transportDefinition = transportSeamSettings.TransportDefinition;
            transportSeamSettings.settings.Set(transportDefinition);

            var settings = new Transport.Settings(hostingConfiguration.EndpointName,
                hostingConfiguration.HostInformation.DisplayName, hostingConfiguration.StartupDiagnostics,
                hostingConfiguration.CriticalError.Raise, hostingConfiguration.ShouldRunInstallers);

            return new TransportSeam(transportDefinition, settings, transportSeamSettings.QueueBindings);
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