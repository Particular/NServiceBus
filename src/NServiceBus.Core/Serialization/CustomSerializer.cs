namespace NServiceBus
{
    using System;
    using NServiceBus.Serialization;

    class CustomSerializer : SerializationDefinition
    {
        protected internal override Type ProvidedByFeature()
        {
            return typeof(CustomSerialization);
        }
    }
}