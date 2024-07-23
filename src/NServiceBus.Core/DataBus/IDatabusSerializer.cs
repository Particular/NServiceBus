namespace NServiceBus.DataBus;

using System;
using System.IO;

/// <summary>
/// Interface used for serializing and deserializing of databus properties.
/// </summary>
public interface IDataBusSerializer
{
    /// <summary>
    /// Serializes the property into the given stream.
    /// </summary>
    /// <param name="databusProperty">The property to serialize.</param>
    /// <param name="stream">The stream to which to write the property.</param>
    void Serialize(object databusProperty, Stream stream);

    /// <summary>
    /// Deserializes a property from the given stream.
    /// </summary>
    /// <param name="stream">The stream from which to read the property.</param>
    /// <param name="propertyType">The type of the property that should be deserialized.</param>
    /// <returns>The deserialized object.</returns>
    object Deserialize(Type propertyType, Stream stream);

    /// <summary>
    /// The content type this serializer handles. Used to populate the <see cref="Headers.DataBusConfigContentType"/> header.
    /// </summary>
    string ContentType { get; }
}