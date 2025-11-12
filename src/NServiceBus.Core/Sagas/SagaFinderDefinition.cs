namespace NServiceBus.Sagas;

using System;

/// <summary>
/// Defines a message finder.
/// </summary>
public partial class SagaFinderDefinition
{
    internal SagaFinderDefinition(ICoreSagaFinder sagaFinder, Type messageType)
    {
        SagaFinder = sagaFinder;
        MessageType = messageType;
        Type = sagaFinder.GetType();
    }

    /// <summary>
    /// The type of message this finder is associated with.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// The finder type.
    /// </summary>
    public Type Type { get; }

    internal ICoreSagaFinder SagaFinder { get; }
}