﻿namespace NServiceBus.Features
{
    using ConsistencyGuarantees;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Transports;

    /// <summary>
    ///     The context available to features when they are activated.
    /// </summary>
    public class FeatureConfigurationContext
    {
        internal FeatureConfigurationContext(Configure config)
        {
            this.config = config;
        }

        /// <summary>
        ///     A read only copy of the settings.
        /// </summary>
        public ReadOnlySettings Settings => config.Settings;

        /// <summary>
        ///     Access to the container to allow for registrations.
        /// </summary>
        public IConfigureComponents Container => config.container;

        /// <summary>
        ///     Access to the pipeline in order to customize it.
        /// </summary>
        public PipelineSettings Pipeline => config.pipelineSettings;

        /// <summary>
        ///     Creates a new satellite processing pipeline.
        /// </summary>
        public PipelineSettings AddSatellitePipeline(string name, string qualifier, ConsistencyGuarantee consistencyGuarantee, PushRuntimeSettings runtimeSettings, out string transportAddress)
        {
            var instanceName = config.Settings.EndpointInstanceName();
            var satelliteLogicalAddress = new LogicalAddress(instanceName, qualifier);
            var addressTranslation = config.Settings.Get<LogicalToTransportAddressTranslation>();
            transportAddress = addressTranslation.Translate(satelliteLogicalAddress);

            var pipelineModifications = new SatellitePipelineModifications(name, transportAddress, consistencyGuarantee, runtimeSettings);
            config.Settings.Get<PipelineConfiguration>().SatellitePipelines.Add(pipelineModifications);
            var newPipeline = new PipelineSettings(pipelineModifications);

            newPipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");
            config.Settings.Get<QueueBindings>().BindReceiving(transportAddress);

            return newPipeline;
        }

        Configure config;
    }
}