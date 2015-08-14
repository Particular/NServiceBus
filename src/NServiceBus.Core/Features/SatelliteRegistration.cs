namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Allows to customize satellite registration.
    /// </summary>
    public class SatelliteRegistration
    {
        readonly Satellite satellite;

        internal SatelliteRegistration(Satellite satellite)
        {
            this.satellite = satellite;
        }

        /// <summary>
        /// Allows to register satellite-specific behaviors that belong only to this satellite.
        /// </summary>
        public PipelineSettings SpecificBehaviors
        {
            get { return new PipelineSettings(satellite.SpecificFeaturesRegistration); }
        }

        /// <summary>
        /// Enables feature <paramref name="featureType"/> for this satellite by registering this feature's behaviors in the satellite pipeline.
        /// </summary>
        /// <param name="featureType">The feature to enable.</param>
        public void EnableFeature(Type featureType)
        {
            satellite.EnableFeature(featureType);
        }

        /// <summary>
        /// Enables feature <typeparamref name="T"/> for this satellite by registering this feature's behaviors in the satellite pipeline.
        /// </summary>
        /// <typeparam name="T">The feature to enable.</typeparam>
        public void EnableFeature<T>() where T : Feature
        {
            satellite.EnableFeature(typeof(T));
        }

    }
}