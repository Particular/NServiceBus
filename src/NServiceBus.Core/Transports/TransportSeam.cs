namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Settings;
using Transport;

class TransportSeam(TransportDefinition transportDefinition, HostingComponent.Configuration hostingConfiguration, SettingsHolder coreSettings, QueueBindings queueBindings)
{
    public void Configure(ReceiveSettings[] receivers) => receiverSettings = receivers;

    // The dependency in IServiceProvider ensures that the TransportInfrastructure can't be resolved too early.
    public TransportInfrastructure GetTransportInfrastructure(IServiceProvider _) => transportInfrastructure;

    public async Task<TransportInfrastructure> CreateTransportInfrastructure(IServiceProvider serviceProvider, bool setupInfrastructure, CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsWindows() && TransportDefinition.TransportTransactionMode == TransportTransactionMode.TransactionScope)
        {
            EnableImplicitDistributedTransactions();
        }

        var hostSettings = new HostSettings(hostingConfiguration.EndpointName,
            hostingConfiguration.HostInformation.DisplayName, hostingConfiguration.StartupDiagnostics,
            hostingConfiguration.CriticalError.Raise, setupInfrastructure, coreSettings)
        {
            ServiceProvider = serviceProvider
        };
        transportInfrastructure = await TransportDefinition.Initialize(hostSettings, receiverSettings, [.. QueueBindings.SendingAddresses], cancellationToken)
            .ConfigureAwait(false);

        return transportInfrastructure;
    }

    [SupportedOSPlatform("windows")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The trimming limitation is conditional on TransactionScope mode and cannot be expressed at the shared transport configuration boundary; the setter is called only for that mode on Windows.")]
    static void EnableImplicitDistributedTransactions() => TransactionManager.ImplicitDistributedTransactions = true;

    public static TransportSeam Create(Settings transportSeamSettings, HostingComponent.Configuration hostingConfiguration)
    {
        var transportDefinition = transportSeamSettings.TransportDefinition;
        transportSeamSettings.settings.Set(transportDefinition);

        var transportSeam = new TransportSeam(transportDefinition, hostingConfiguration, transportSeamSettings.settings, transportSeamSettings.QueueBindings);

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
            get => !settings.HasExplicitValue<TransportDefinition>() ? throw new Exception("A transport has not been configured. Use 'EndpointConfiguration.UseTransport()' to specify a transport.") : settings.Get<TransportDefinition>();
            set => settings.Set(value);
        }

        public QueueBindings QueueBindings => settings.Get<QueueBindings>();

        internal readonly SettingsHolder settings;
    }
}