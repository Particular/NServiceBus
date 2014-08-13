namespace NServiceBus.Serialization
{
    using System;
    using NServiceBus.Settings;

    /// <summary>
    /// Implemented by serializers to provide their capabilities
    /// </summary>
    public abstract class SerializationDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SerializationDefinition"/>.
        /// </summary>
        protected SerializationDefinition(SettingsHolder settings)
        {
            Settings = settings;
        }

        /// <summary>
        /// The feature to enable when this serializer is selected
        /// </summary>
        internal abstract Type ProvidedByFeature();

        /// <summary>
        /// Get the current <see cref="SettingsHolder"/> this <see cref="SerializationDefinition"/> wraps.
        /// </summary>
        protected SettingsHolder Settings { get; private set; }
    }

}