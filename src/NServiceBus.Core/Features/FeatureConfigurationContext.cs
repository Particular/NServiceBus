namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Transports;
    using ObjectBuilder;
    using Pipeline;
    using Settings;

    /// <summary>
    /// The context available to features when they are activated
    /// </summary>
    public class FeatureConfigurationContext
    {
        readonly Configure config;

        internal FeatureConfigurationContext(Configure config)
        {
            this.config = config;            
        }

        /// <summary>
        /// A read only copy of the settings
        /// </summary>
        public ReadOnlySettings Settings { get { return config.Settings; } }
        
        /// <summary>
        /// Access to the container to allow for registrations
        /// </summary>
        public IConfigureComponents Container { get { return config.container; } }
        
        /// <summary>
        /// Access to the pipeline in order to customize it
        /// </summary>
        public PipelineSettings PipelinesCollection { get { return config.pipelineSettings; } }

        /// <summary>
        /// Registers the receive behavior to use for that endpoint.
        /// </summary>
        /// <param name="receiveBehaviorFactory"></param>
        public void RegisterReceiveBehavior(Func<IBuilder, ReceiveBehavior> receiveBehaviorFactory)
        {
            var receiveBehavior = new ReceiveBehavior.Registration();
            receiveBehavior.ContainerRegistration((b, s) => receiveBehaviorFactory(b));
            config.Settings.Get<PipelinesCollection>().ReceiveBehavior = receiveBehavior;
        }

        /// <summary>
        /// Creates a new processing pipeline.
        /// </summary>
        /// <returns></returns>
        public PipelineSettings AddSatellitePipeline(string name, string receiveAddress)
        {
            var pipelineModifications = new SatellitePipelineModifications(name, receiveAddress);
            config.Settings.Get<PipelinesCollection>().SatellitePipelines.Add(pipelineModifications);
            var newPipeline = new PipelineSettings(pipelineModifications);

            newPipeline.RegisterConnector<TransportReceiveToPhysicalMessageProcessingConnector>("Allows to abort processing the message");

            return newPipeline;
        }
    }
}