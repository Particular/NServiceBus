namespace NServiceBus
{
    using System;
    using Features;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the JSON serializer.
    /// </summary>
    public class JsonSerializer : SerializationDefinition
    {
        /// <summary>
        /// <see cref="SerializationDefinition.ProvidedByFeature"/>.
        /// </summary>
        protected internal override Type ProvidedByFeature()
        {
            return typeof(JsonSerialization);
        }

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to.
        /// </summary>
        public override string ContentType
        {
            get { return ContentTypes.Json; }
        }
    }
}