namespace NServiceBus
{
    using System;
    using Features;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the Binary serializer
    /// </summary>
    public class BinarySerializer : SerializationDefinition
    {
        /// <summary>
        /// <see cref="SerializationDefinition.ProvidedByFeature"/>
        /// </summary>
        protected internal override Type ProvidedByFeature()
        {
            return typeof(BinarySerialization);
        }
    }
}