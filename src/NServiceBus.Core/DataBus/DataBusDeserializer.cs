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

        public object Deserialize(string serializerUsed, Type type, Stream stream)
        {
            if (string.IsNullOrEmpty(serializerUsed))
            {

                try
                {
                    return mainSerializer.Deserialize(type, stream);
                }
                catch (Exception ex)
                {
                    if (fallbackSerializer == null)
                    {
                        throw;
                    }
                    stream.Position = 0;
                    logger.Info($"Failed to deserialize data bus property using the main {mainSerializer.Name} serializer.", ex);

                    return fallbackSerializer.Deserialize(type, stream);
                }
            }

            if (mainSerializer.Name == serializerUsed)
            {
                return mainSerializer.Deserialize(type, stream);
            }

            if (fallbackSerializer?.Name != serializerUsed)
            {
                throw new Exception($"Serializer {serializerUsed} not configured.");
            }

            return fallbackSerializer.Deserialize(type, stream);
        }

        readonly IDataBusSerializer mainSerializer;
        readonly IDataBusSerializer fallbackSerializer;

        static readonly ILog logger = LogManager.GetLogger<DataBusDeserializer>();
    }
}