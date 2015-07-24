namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Transports;
    using ObjectBuilder;
    using Pipeline;
    using Settings;

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
        /// Registers the receive behavior to use for that endpoint.
        /// </summary>
        public void RegisterReceiveBehavior(Func<IBuilder, ReceiveBehavior> receiveBehaviorFactory)
        {
            var receiveBehavior = new ReceiveBehavior.Registration();
            receiveBehavior.ContainerRegistration((b, s) => receiveBehaviorFactory(b));
            config.Settings.Get<PipelineConfiguration>().ReceiveBehavior = receiveBehavior;
        }

        /// <summary>
        /// Creates a new satellite processing pipeline.
        /// </summary>
        public PipelineSettings AddSatellitePipeline(string name, string receiveAddress)
        {
            var pipelineModifications = new SatellitePipelineModifications(name, receiveAddress);
            config.Settings.Get<PipelineConfiguration>().SatellitePipelines.Add(pipelineModifications);
            var newPipeline = new PipelineSettings(pipelineModifications);

            newPipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");

            config.Settings.Get<QueueBindings>().BindReceiving(receiveAddress);

            return newPipeline;
        }
    }
}