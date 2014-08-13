namespace NServiceBus
{
    using System;
    using Features;
    using NServiceBus.Settings;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the JSON serializer
    /// </summary>
    public class Json : SerializationDefinition
    {
        
        /// <summary>
        /// Initialise a new instance of <see cref="Json"/>.
        /// </summary>
        /// <param name="settings"></param>
        public Json(SettingsHolder settings)
            : base(settings)
        {
        }

        /// <summary>
        /// <see cref="SerializationDefinition.ProvidedByFeature"/>
        /// </summary>
        internal override Type ProvidedByFeature()
        {
            return typeof(JsonSerialization);
        }
    }
}