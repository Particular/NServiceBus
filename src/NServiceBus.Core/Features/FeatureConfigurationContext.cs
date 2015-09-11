namespace NServiceBus.Features
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// The context available to features when they are activated.
    /// </summary>
    public class FeatureConfigurationContext
    {
        Configure config;

        internal FeatureConfigurationContext(Configure config)
        {
            this.config = config;
        }

        /// <summary>
        /// A read only copy of the settings.
        /// </summary>
        public ReadOnlySettings Settings { get { return config.Settings; } }

        /// <summary>
        /// Access to the container to allow for registrations.
        /// </summary>
        public IConfigureComponents Container { get { return config.container; } }

        /// <summary>
        /// Access to the pipeline in order to customize it.
        /// </summary>
        public PipelineSettings Pipeline { get { return config.pipelineSettings; } }

        /// <summary>
        /// Creates a new satellite processing pipeline.
        /// </summary>
        public PipelineSettings AddSatellitePipeline(string name, string qualifier, out string transportAddress, int? maxConcurrency = null, Unicast.Transport.TransactionSettings transactionSettings = null)
        {
            var instanceName = config.Settings.EndpointInstanceName();
            var satelliteLogicalAddress = new LogicalAddress(instanceName, qualifier);
            var addressTranslation = config.Settings.Get<LogicalToTransportAddressTranslation>();
            transportAddress = addressTranslation.Translate(satelliteLogicalAddress);

            var pipelineModifications = new SatellitePipelineModifications(name, transportAddress, transactionSettings, maxConcurrency.HasValue ? new PushRuntimeSettings(maxConcurrency.Value) : null);
            config.Settings.Get<PipelineConfiguration>().SatellitePipelines.Add(pipelineModifications);
            var newPipeline = new PipelineSettings(pipelineModifications);

            newPipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
            config.Settings.Get<QueueBindings>().BindReceiving(transportAddress);

            return newPipeline;
        }
    }
}