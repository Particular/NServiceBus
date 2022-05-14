namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.DataBus;

    class DataBusDeserializer
    {
        public DataBusDeserializer(IList<IDataBusSerializer> serializers)
        {
            foreach (var serializer in serializers)
            {
                availableSerializers[serializer.Name] = serializer;
            }
        }
        public object Deserialize(string serializerUsed, Type type, Stream stream)
        {
            return availableSerializers[serializerUsed].Deserialize(type, stream);
        }

        readonly IDictionary<string, IDataBusSerializer> availableSerializers = new Dictionary<string, IDataBusSerializer>();
    }
}