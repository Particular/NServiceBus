namespace NServiceBus.Features
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    ///     The context available to features when they are activated.
    /// </summary>
    public class FeatureConfigurationContext
    {
        internal FeatureConfigurationContext(ReadOnlySettings settings, IConfigureComponents container, PipelineSettings pipelineSettings)
        {
            this.Settings = settings;
            this.Container = container;
            this.Pipeline = pipelineSettings;
        }

        /// <summary>
        ///     A read only copy of the settings.
        /// </summary>
        public ReadOnlySettings Settings { get; }

        /// <summary>
        ///     Access to the container to allow for registrations.
        /// </summary>
        public IConfigureComponents Container { get; }

        /// <summary>
        ///     Access to the pipeline in order to customize it.
        /// </summary>
        public PipelineSettings Pipeline { get; }

        /// <summary>
        ///     Creates a new satellite processing pipeline.
        /// </summary>
        public PipelineSettings AddSatellitePipeline(string name, string qualifier, TransactionSupport requiredTransactionSupport, PushRuntimeSettings runtimeSettings, out string transportAddress)
        {
            var instanceName = Settings.EndpointInstanceName();
            var satelliteLogicalAddress = new LogicalAddress(instanceName, qualifier);
            var addressTranslation = Settings.Get<LogicalToTransportAddressTranslation>();
            transportAddress = addressTranslation.Translate(satelliteLogicalAddress);

            var pipelineModifications = new SatellitePipelineModifications(name, transportAddress, requiredTransactionSupport, runtimeSettings);
            Settings.Get<PipelineConfiguration>().SatellitePipelines.Add(pipelineModifications);
            var newPipeline = new PipelineSettings(pipelineModifications);

            newPipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
            Settings.Get<QueueBindings>().BindReceiving(transportAddress);

            return newPipeline;
        }
    }
}