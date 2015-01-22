namespace NServiceBus.Features
{
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
        public PipelineSettings Pipeline { get { return config.pipeline; } }
    }
}