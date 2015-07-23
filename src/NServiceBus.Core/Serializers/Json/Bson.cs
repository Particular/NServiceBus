namespace NServiceBus
{
    using System;
    using Features;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the BSON serializer.
    /// </summary>
    public class BsonSerializer : SerializationDefinition
    {
        /// <summary>
        /// <see cref="SerializationDefinition.ProvidedByFeature"/>.
        /// </summary>
        protected internal override Type ProvidedByFeature()
        {
            return typeof(BsonSerialization);
        }

    }
}