namespace NServiceBus.Serialization
{
    using System;
    using Features;

    public abstract class SerializationDefinition<T> : ISerializationDefinition where T : Feature
    {
        public Type ProvidedByFeature
        {
            get { return typeof(T); }
        }
    }

    public interface ISerializationDefinition
    {
        Type ProvidedByFeature { get; }
    }
}