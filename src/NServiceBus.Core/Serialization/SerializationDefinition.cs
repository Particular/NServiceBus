namespace NServiceBus.Serialization;

using System;
using MessageInterfaces;
using Settings;

/// <summary>
/// Implemented by serializers to provide their capabilities.
/// </summary>
public abstract class SerializationDefinition
{
    /// <summary>
    /// Provides a factory method for building a message serializer.
    /// </summary>
    public abstract Func<IMessageMapper, IMessageSerializer> Configure(IReadOnlySettings settings);

    /// <summary>
    /// Gets or sets a value indicating whether the serializer supports zero-length messages.
    /// NOTE Hack for proof of concept.
    /// </summary>  
    public virtual bool SupportsZeroLengthMessages { get; set; } = false;
}