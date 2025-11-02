namespace NServiceBus.Sagas;

using System;

/// <summary>
/// Defines a message finder.
/// </summary>
public class SagaFinderDefinition
{
    internal SagaFinderDefinition(ICoreSagaFinder sagaFinder, Type messageType)
    {
        SagaFinder = sagaFinder;
        Type = sagaFinder.GetType();
        MessageType = messageType;
        MessageTypeName = messageType.FullName;
    }

    /// <summary>
    /// The type of the finder.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The type of message this finder is associated with.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// The full name of the message type.
    /// </summary>
    public string MessageTypeName { get; }

    internal ICoreSagaFinder SagaFinder { get; }
}