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
        readonly IConfigureComponents configureComponents;
        readonly ReadOnlySettings settings;
        readonly PipelineSettings pipelineSettings;

        internal FeatureConfigurationContext(IConfigureComponents configureComponents, ReadOnlySettings settings, PipelineModificationsBuilder pipelineModificationsBuilder)
        {
            this.configureComponents = configureComponents;
            this.settings = settings;
            pipelineSettings = new PipelineSettings(pipelineModificationsBuilder);
        }

        /// <summary>
        /// A read only copy of the settings.
        /// </summary>
        public ReadOnlySettings Settings { get { return settings; } }
        
        /// <summary>
        /// Access to the container to allow for registrations.
        /// </summary>
        public IConfigureComponents Container { get { return configureComponents; } }

        /// <summary>
        /// Access to the pipeline in order to customize it.
        /// </summary>
        public PipelineSettings Pipeline { get { return pipelineSettings; } }

        /// <summary>
        /// Registers the receive behavior to use for that endpoint.
        /// </summary>
        public void RegisterReceiveBehavior(Func<IBuilder, ReceiveBehavior> receiveBehaviorFactory)
        {
            var receiveBehavior = new ReceiveBehavior.Registration();
            receiveBehavior.ContainerRegistration((b, s) => receiveBehaviorFactory(b));
            settings.Get<PipelineConfiguration>().ReceiveBehavior = receiveBehavior;
        }

        /// <summary>
        /// Creates a new satellite processing pipeline.
        /// </summary>
        public SatelliteRegistration AddSatellitePipeline(string name, string receiveAddress, RegisterStep pipelineBehaviorRegistration)
        {
            var satellite = new Satellite(name, receiveAddress, pipelineBehaviorRegistration);

            settings.Get<SatelliteCollection>().Register(satellite);
            settings.Get<QueueBindings>().BindReceiving(receiveAddress);

            return new SatelliteRegistration(satellite);
        }
    }
}