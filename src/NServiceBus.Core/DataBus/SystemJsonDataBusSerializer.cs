namespace NServiceBus;

using System;
using System.IO;
using System.Text.Json;
using DataBus;

/// <summary>
/// Data bus serialization using the <see cref="JsonSerializer"/> serializer.
/// </summary>
[ObsoleteEx(
    Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
    RemoveInVersion = "11",
    TreatAsErrorFromVersion = "10")]
public class SystemJsonDataBusSerializer : IDataBusSerializer
{
    /// <summary>
    /// Serializes the property.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public void Serialize(object dataBusProperty, Stream stream)
    {
        JsonSerializer.Serialize(stream, dataBusProperty);
    }

    /// <summary>
    /// Deserializes the property.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public object Deserialize(Type propertyType, Stream stream)
    {
        return JsonSerializer.Deserialize(stream, propertyType);
    }

    /// <summary>
    /// The content type this serializer handles. Used to populate the <see cref="Headers.DataBusConfigContentType"/> header.
    /// </summary>
    [ObsoleteEx(
        Message = "The DataBus feature is released as a dedicated 'NServiceBus.ClaimCheck.DataBus' package.",
        RemoveInVersion = "11",
        TreatAsErrorFromVersion = "10")]
    public string ContentType { get; } = "application/json";
}
