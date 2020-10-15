using System.Threading.Tasks;

namespace NServiceBus
{
    using System;
    using Settings;
    using Transport;

    class TransportSeam
    {
        protected TransportSeam(TransportInfrastructure transportInfrastructure, QueueBindings queueBindings)
        {
            TransportInfrastructure = transportInfrastructure;
            QueueBindings = queueBindings;
        }

        public static async Task<TransportSeam> Create(Settings transportSettings, HostingComponent.Configuration hostingConfiguration)
        {
            var transportDefinition = transportSettings.TransportDefinition;

            var transportInfrastructure = await transportDefinition.Initialize(
                    new Transport.Settings(hostingConfiguration.EndpointName,
                        hostingConfiguration.HostInformation.DisplayName, hostingConfiguration.StartupDiagnostics,
                        hostingConfiguration.CriticalError.Raise, hostingConfiguration.ShouldRunInstallers))
                .ConfigureAwait(false);

            //RegisterTransportInfrastructureForBackwardsCompatibility
            transportSettings.settings.Set(transportInfrastructure);

            hostingConfiguration.AddStartupDiagnosticsSection("Transport", new
            {
                Type = transportInfrastructure.GetType().FullName,
                Version = FileVersionRetriever.GetFileVersion(transportInfrastructure.GetType())
            });

            return new TransportSeam(transportInfrastructure, transportSettings.QueueBindings);
        }

        public TransportInfrastructure TransportInfrastructure { get; }

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