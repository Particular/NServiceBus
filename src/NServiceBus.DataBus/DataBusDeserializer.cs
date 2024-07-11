namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using NServiceBus.DataBus;
using NServiceBus.Logging;

class DataBusDeserializer
{
    public DataBusDeserializer(
        IDataBusSerializer mainDeserializer,
        IReadOnlyCollection<IDataBusSerializer> additionalDeserializers)
    {
        deserializers = new Dictionary<string, IDataBusSerializer>
        {
            { mainDeserializer.ContentType, mainDeserializer }
        };

        foreach (var additionalDeserializer in additionalDeserializers)
        {
            deserializers.Add(additionalDeserializer.ContentType, additionalDeserializer);
        }
    }

    public object Deserialize(string serializerUsed, Type propertyType, Stream stream)
    {
        if (string.IsNullOrEmpty(serializerUsed))
        {
            foreach (var deserializerToTry in deserializers.Values)
            {
                try
                {
                    return deserializerToTry.Deserialize(propertyType, stream);
                }
                catch (Exception ex)
                {
                    logger.Info($"Failed to deserialize data bus property using the main '{deserializerToTry.ContentType}' serializer.", ex);

                    stream.Position = 0;
                }
            }

            throw new Exception($"None of the configured serializers for {string.Join(", ", deserializers.Keys)} where able to deserialize the data bus property.");
        }

        if (!deserializers.TryGetValue(serializerUsed, out var deserializer))
        {

            throw new Exception($"Serializer for content type {serializerUsed} not configured.");
        }

        return deserializer.Deserialize(propertyType, stream);
    }

    readonly Dictionary<string, IDataBusSerializer> deserializers;

    static readonly ILog logger = LogManager.GetLogger<DataBusDeserializer>();
}