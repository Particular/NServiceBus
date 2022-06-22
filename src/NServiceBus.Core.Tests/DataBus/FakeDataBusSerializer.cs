namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using NServiceBus.DataBus;

    public class FakeDataBusSerializer : IDataBusSerializer
    {
        public FakeDataBusSerializer(string contentType = "some-content-type", bool throwOnDeserialize = false)
        {
            ContentType = contentType;
            this.throwOnDeserialize = throwOnDeserialize;
        }
        public string ContentType { get; }

        public object Deserialize(Type propertyType, Stream stream)
        {
            if (throwOnDeserialize)
            {
                throw new SerializationException();
            }

            return "test";
        }

        public void Serialize(object databusProperty, Stream stream)
        {
        }

        readonly bool throwOnDeserialize;
    }
}