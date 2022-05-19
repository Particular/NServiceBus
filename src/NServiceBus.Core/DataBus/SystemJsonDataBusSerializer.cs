namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Text.Json;
    using DataBus;

    /// <summary>
    /// Data bus serialization using the <see cref="JsonSerializer"/> serializer.
    /// </summary>
    public class SystemJsonDataBusSerializer : IDataBusSerializer
    {
        /// <summary>
        /// Serializes the property.
        /// </summary>
        public void Serialize(object databusProperty, Stream stream)
        {
            JsonSerializer.Serialize(stream, databusProperty);
        }

        /// <summary>
        /// Deserializes the property.
        /// </summary>
        public object Deserialize(Type propertyType, Stream stream)
        {
            return JsonSerializer.Deserialize(stream, propertyType);
        }

        /// <summary>
        /// The name of this serializer. Used to populate the NServiceBus.Databus.Serializer header.
        /// </summary>
        public string Name { get; } = "system-json";
    }
}
