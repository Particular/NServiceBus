namespace NServiceBus.Serialization
{
    using System;
    using Features;

    /// <summary>
    /// Implemented by serializers to provide their capabilities
    /// </summary>
    /// <typeparam name="T">The type of serialization to provide</typeparam>
    public abstract class SerializationDefinition<T> : ISerializationDefinition where T : Feature
    {
        /// <summary>
        /// The feature to enable when this serializer is selected
        /// </summary>
        public Type ProvidedByFeature
        {
            get { return typeof(T); }
        }
    }

    /// <summary>
    /// Implemented by serializers to provide their capabilities
    /// </summary>
    public interface ISerializationDefinition
    {
        /// <summary>
        /// The feature to enable when this serializer is selected
        /// </summary>
        Type ProvidedByFeature { get; }
    }
}