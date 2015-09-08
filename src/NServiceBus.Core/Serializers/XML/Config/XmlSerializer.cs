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
    }
}