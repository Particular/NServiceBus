namespace NServiceBus
{
    using System;
    using Features;
    using NServiceBus.Settings;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the Binary serializer
    /// </summary>
    public class Binary : SerializationDefinition
    {
        /// <summary>
        /// Initialise a new instance of <see cref="Binary"/>.
        /// </summary>
        /// <param name="settings"></param>
        public Binary(SettingsHolder settings) : base(settings)
        {
        }

        /// <summary>
        /// <see cref="SerializationDefinition.ProvidedByFeature"/>
        /// </summary>
        internal override Type ProvidedByFeature()
        {
            return typeof(BinarySerialization);
        }
    }
}