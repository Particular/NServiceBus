namespace NServiceBus
{
    using System;
    using Features;
    using Serialization;

    /// <summary>
    /// Defines the capabilities of the XML serializer.
    /// </summary>
    public class XmlSerializer : SerializationDefinition
    {
        /// <summary>
        /// The feature to enable when this serializer is selected.
        /// </summary>
        protected internal override Type ProvidedByFeature()
        {
            return typeof(XmlSerialization);
        }

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to.
        /// </summary>
        public override string ContentType
        {
            get { return ContentTypes.Xml; }
        }
    }
}