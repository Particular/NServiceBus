namespace NServiceBus.Sagas;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;

/// <summary>
/// Defines a message finder.
/// </summary>
public class SagaFinderDefinition
{
    readonly ICoreSagaFinder sagaFinder;

    internal SagaFinderDefinition(ICoreSagaFinder sagaFinder, Type messageType, Dictionary<string, object> properties)
    {
        this.sagaFinder = sagaFinder;
        Type = sagaFinder.GetType();
        MessageType = messageType;
        MessageTypeName = messageType.FullName;
        Properties = properties;
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

    /// <summary>
    /// Custom properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; }

    internal Task<IContainSagaData> InvokeFinder(IServiceProvider serviceProvider,
        ISynchronizedStorageSession synchronizedStorageSession,
        ContextBag context,
        object message,
        IReadOnlyDictionary<string, string> messageHeaders,
        CancellationToken cancellationToken = default) =>
        sagaFinder.Find(serviceProvider, this, synchronizedStorageSession, context, message, messageHeaders, cancellationToken);
}