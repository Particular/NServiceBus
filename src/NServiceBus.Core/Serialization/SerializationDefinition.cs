namespace NServiceBus.Serialization
{
    using System;

    /// <summary>
    /// Implemented by serializers to provide their capabilities
    /// </summary>
    public abstract class SerializationDefinition
    {
        /// <summary>
        /// The feature to enable when this serializer is selected
        /// </summary>
        internal abstract Type ProvidedByFeature();
    }
}