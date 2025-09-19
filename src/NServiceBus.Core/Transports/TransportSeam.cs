namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Settings;
using Transport;

class TransportSeam(TransportDefinition transportDefinition, HostSettings hostSettings, QueueBindings queueBindings)
{
    public void Configure(ReceiveSettings[] receivers) => receiverSettings = receivers;

    // The dependency in IServiceProvider ensures that the TransportInfrastructure can't be resolved too early.
    public TransportInfrastructure GetTransportInfrastructure(IServiceProvider _) => transportInfrastructure;

    public async Task<TransportInfrastructure> CreateTransportInfrastructure(CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsWindows() && TransportDefinition.TransportTransactionMode == TransportTransactionMode.TransactionScope)
        {
            TransactionManager.ImplicitDistributedTransactions = true;
        }

        transportInfrastructure = await TransportDefinition.Initialize(hostSettings, receiverSettings, [.. QueueBindings.SendingAddresses], cancellationToken)
            .ConfigureAwait(false);

        return transportInfrastructure;
    }

    public static TransportSeam Create(Settings transportSeamSettings, HostingComponent.Configuration hostingConfiguration)
    {
        var transportDefinition = transportSeamSettings.TransportDefinition;
        transportSeamSettings.settings.Set(transportDefinition);

        var settings = new HostSettings(hostingConfiguration.EndpointName, hostingConfiguration.HostInformation.DisplayName,
            hostingConfiguration.StartupDiagnostics, hostingConfiguration.Manifest,
            hostingConfiguration.CriticalError.Raise, hostingConfiguration.ShouldRunInstallers,
            transportSeamSettings.settings);

        var transportSeam = new TransportSeam(transportDefinition, settings, transportSeamSettings.QueueBindings);

        hostingConfiguration.Services.AddSingleton(_ => transportSeam.transportInfrastructure.Dispatcher);

        hostingConfiguration.Services.AddSingleton<ITransportAddressResolver>(serviceProvider =>
            new TransportAddressResolver(transportSeam, serviceProvider));

        return transportSeam;
    }

    public TransportDefinition TransportDefinition { get; } = transportDefinition;

    public QueueBindings QueueBindings { get; } = queueBindings;

    ReceiveSettings[] receiverSettings;
    TransportInfrastructure transportInfrastructure;

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