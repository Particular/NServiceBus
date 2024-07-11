namespace NServiceBus;

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
    public void Serialize(object dataBusProperty, Stream stream)
    {
        JsonSerializer.Serialize(stream, dataBusProperty);
    }

    /// <summary>
    /// Deserializes the property.
    /// </summary>
    public object Deserialize(Type propertyType, Stream stream)
    {
        return JsonSerializer.Deserialize(stream, propertyType);
    }

    /// <summary>
    /// The content type this serializer handles. Used to populate the <see cref="Headers.DataBusConfigContentType"/> header.
    /// </summary>
    public string ContentType { get; } = "application/json";
}
