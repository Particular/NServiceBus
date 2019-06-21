namespace NServiceBus
{
    using System.Linq;
    using Features;
    using MessageInterfaces.MessageMapper.Reflection;
    using ObjectBuilder;
    using Settings;
    using Transport;

    class ConfiguredEndpoint : IConfiguredEndpoint
    {
        public ConfiguredEndpoint(SettingsHolder settings, FeatureActivator featureActivator, TransportInfrastructure transportInfrastructure, ReceiveConfiguration receiveConfiguration, CriticalError criticalError, QueueBindings queueBindings, EventAggregator eventAggregator, MessageMapper messageMapper, PipelineConfiguration pipelineConfiguration)
        {
            this.criticalError = criticalError;
            this.queueBindings = queueBindings;
            this.eventAggregator = eventAggregator;
            this.messageMapper = messageMapper;
            this.pipelineConfiguration = pipelineConfiguration;
            this.settings = settings;
            this.featureActivator = featureActivator;
            this.transportInfrastructure = transportInfrastructure;
            this.receiveConfiguration = receiveConfiguration;
        }

        public IInstallableEndpoint UseBuilder(IBuilder builder)
        {
            var pipelineCache = new PipelineCache(builder, settings);
            var receiveComponent = CreateReceiveComponent(pipelineCache);

            var messageSession = new MessageSession(new RootContext(builder, pipelineCache, eventAggregator, messageMapper));

            return new InstallableEndpoint(settings, builder, featureActivator, transportInfrastructure, receiveComponent, criticalError, messageSession);
        }

        ReceiveComponent CreateReceiveComponent(
            IPipelineCache pipelineCache)
        {
            var errorQueue = settings.ErrorQueueAddress();

            var receiveComponent = new ReceiveComponent(receiveConfiguration,
                receiveConfiguration != null ? transportInfrastructure.ConfigureReceiveInfrastructure() : null, //don't create the receive infrastructure for send-only endpoints
                pipelineCache,
                pipelineConfiguration,
                eventAggregator,
                criticalError,
                errorQueue,
                messageMapper);

            receiveComponent.BindQueues(queueBindings);

            if (receiveConfiguration != null)
            {
                settings.AddStartupDiagnosticsSection("Receiving", new
                {
                    receiveConfiguration.LocalAddress,
                    receiveConfiguration.InstanceSpecificQueue,
                    receiveConfiguration.LogicalAddress,
                    receiveConfiguration.PurgeOnStartup,
                    receiveConfiguration.QueueNameBase,
                    TransactionMode = receiveConfiguration.TransactionMode.ToString("G"),
                    receiveConfiguration.PushRuntimeSettings.MaxConcurrency,
                    Satellites = receiveConfiguration.SatelliteDefinitions.Select(s => new
                    {
                        s.Name,
                        s.ReceiveAddress,
                        TransactionMode = s.RequiredTransportTransactionMode.ToString("G"),
                        s.RuntimeSettings.MaxConcurrency
                    }).ToArray()
                });
            }

            return receiveComponent;
        }

        FeatureActivator featureActivator;
        SettingsHolder settings;
        TransportInfrastructure transportInfrastructure;
        ReceiveConfiguration receiveConfiguration;
        CriticalError criticalError;
        QueueBindings queueBindings;
        EventAggregator eventAggregator;
        MessageMapper messageMapper;
        PipelineConfiguration pipelineConfiguration;
    }
}