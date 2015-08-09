namespace NServiceBus
{
    using System;
    using Features;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the Binary serializer.
    /// </summary>
    public class BinarySerializer : SerializationDefinition
    {
        /// <summary>
        /// <see cref="SerializationDefinition.ProvidedByFeature"/>.
        /// </summary>
        protected internal override Type ProvidedByFeature()
        {
            return typeof(BinarySerialization);
        }

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to.
        /// </summary>
        public override string ContentType
        {
            get { return ContentTypes.Binary; }
        }
    }
}