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

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to.
        /// </summary>
        public override string ContentType
        {
            get { return ContentTypes.Bson; }
        }
    }
}