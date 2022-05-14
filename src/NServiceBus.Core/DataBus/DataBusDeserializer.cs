namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NServiceBus.DataBus;
    using NServiceBus.Logging;

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
            if (string.IsNullOrEmpty(serializerUsed))
            {
                foreach (var serializerToTry in availableSerializers.Values)
                {
                    try
                    {
                        return serializerToTry.Deserialize(type, stream);
                    }
                    catch (Exception)
                    {
                        stream.Position = 0;
                        logger.Info($"Failed to deserialize data bus property using the {serializerToTry.Name} serializer.");
                    }
                }

                var triedSerializers = string.Join(",", availableSerializers.Values.Select(s => s.Name));

                throw new Exception($"None of the {triedSerializers} serializers was able to deserialize the data bus property.");
            }

            if (!availableSerializers.TryGetValue(serializerUsed, out var serializer))
            {
                throw new Exception($"Serializer {serializerUsed} not configured.");
            }

            return serializer.Deserialize(type, stream);
        }

        readonly IDictionary<string, IDataBusSerializer> availableSerializers = new Dictionary<string, IDataBusSerializer>();

        static readonly ILog logger = LogManager.GetLogger<DataBusDeserializer>();
    }
}