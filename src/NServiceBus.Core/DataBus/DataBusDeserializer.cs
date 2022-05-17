namespace NServiceBus
{
    using System;
    using System.IO;
    using NServiceBus.DataBus;
    using NServiceBus.Logging;

    class DataBusDeserializer
    {
        public DataBusDeserializer(
            IDataBusSerializer mainSerializer,
            IDataBusSerializer fallbackSerializer)
        {
            this.mainSerializer = mainSerializer;
            this.fallbackSerializer = fallbackSerializer;
        }

        public object Deserialize(string serializerUsed, Type propertyType, Stream stream)
        {
            if (string.IsNullOrEmpty(serializerUsed))
            {
                try
                {
                    return mainSerializer.Deserialize(propertyType, stream);
                }
                catch (Exception ex)
                {
                    if (fallbackSerializer == null)
                    {
                        throw;
                    }

                    logger.Info($"Failed to deserialize data bus property using the main {mainSerializer.Name} serializer.", ex);

                    return TryFallbackSerializer(propertyType, stream);
                }
            }

            if (mainSerializer.Name == serializerUsed)
            {
                return mainSerializer.Deserialize(propertyType, stream);
            }

            if (fallbackSerializer?.Name != serializerUsed)
            {
                throw new Exception($"Serializer {serializerUsed} not configured.");
            }

            return fallbackSerializer.Deserialize(propertyType, stream);
        }

        [ObsoleteEx(Message = "No fallback serializer is needed in version 9 so this can be safely removed.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        object TryFallbackSerializer(Type propertyType, Stream stream)
        {
            stream.Position = 0;

            return fallbackSerializer.Deserialize(propertyType, stream);
        }

        readonly IDataBusSerializer mainSerializer;
        readonly IDataBusSerializer fallbackSerializer;

        static readonly ILog logger = LogManager.GetLogger<DataBusDeserializer>();
    }
}