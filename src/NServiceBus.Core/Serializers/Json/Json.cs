namespace NServiceBus
{
    using System;
    using Features;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the JSON serializer
    /// </summary>
    public class JsonSerializer : SerializationDefinition
    {
        /// <summary>
        /// <see cref="SerializationDefinition.ProvidedByFeature"/>
        /// </summary>
        internal override Type ProvidedByFeature()
        {
            return typeof(JsonSerialization);
        }
    }
}