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
        MessageType = messageType;
    }

    /// <summary>
    /// The type of message this finder is associated with.
    /// </summary>
    public Type MessageType { get; }

    internal ICoreSagaFinder SagaFinder { get; }
}