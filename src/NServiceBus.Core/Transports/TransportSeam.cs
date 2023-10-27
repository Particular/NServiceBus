﻿namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Microsoft.Extensions.DependencyInjection;
    using Settings;
    using Transport;

    class TransportSeam
    {
        readonly HostSettings hostSettings;
        ReceiveSettings[] receivers;

        protected TransportSeam(TransportDefinition transportDefinition, HostSettings hostSettings,
            QueueBindings queueBindings)
        {
            TransportDefinition = transportDefinition;
            QueueBindings = queueBindings;
            this.hostSettings = hostSettings;
        }

        public void Configure(ReceiveSettings[] receivers)
        {
            this.receivers = receivers;
        }

        // The dependency in IServiceProvider ensures that the TransportInfrastructure can't be resolved too early.
        public TransportInfrastructure GetTransportInfrastructure(IServiceProvider _) => TransportInfrastructure;

#pragma warning disable CA1822 // Mark members as static
        public ITransportAddressResolver TransportAddressResolverBuilder(IServiceProvider sp) => sp.GetRequiredService<ITransportAddressResolver>();
#pragma warning restore CA1822 // Mark members as static

        public async Task<TransportInfrastructure> CreateTransportInfrastructure(CancellationToken cancellationToken = default)
        {
            if (OperatingSystem.IsWindows() && TransportDefinition.TransportTransactionMode == TransportTransactionMode.TransactionScope)
            {
                TransactionManager.ImplicitDistributedTransactions = true;
            }

            TransportInfrastructure = await TransportDefinition.Initialize(hostSettings, receivers, QueueBindings.SendingAddresses.ToArray(), cancellationToken)
                .ConfigureAwait(false);

            return TransportInfrastructure;
        }

        public static TransportSeam Create(Settings transportSeamSettings, HostingComponent.Configuration hostingConfiguration)
        {
            var transportDefinition = transportSeamSettings.TransportDefinition;
            transportSeamSettings.settings.Set(transportDefinition);

            var settings = new HostSettings(hostingConfiguration.EndpointName,
                hostingConfiguration.HostInformation.DisplayName, hostingConfiguration.StartupDiagnostics,
                hostingConfiguration.CriticalError.Raise, hostingConfiguration.ShouldRunInstallers,
                transportSeamSettings.settings);

            var transportSeam = new TransportSeam(transportDefinition, settings, transportSeamSettings.QueueBindings);

            hostingConfiguration.Services.AddSingleton(_ => transportSeam.TransportInfrastructure.Dispatcher);

            hostingConfiguration.Services.AddSingleton<ITransportAddressResolver>(_ =>
                new TransportAddressResolver(transportSeam.TransportInfrastructure));

            return transportSeam;
        }

        TransportInfrastructure TransportInfrastructure { get; set; }

        public TransportDefinition TransportDefinition { get; }

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