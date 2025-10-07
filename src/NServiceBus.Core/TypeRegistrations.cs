namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides a way to register NServiceBus extension types without assembly scanning.
/// Used by source-generated code to register handlers, features, messages, etc.
/// </summary>
public sealed class TypeRegistrations
{
    // Storage: marker type → list of implementations
    // Example: IHandleMessages → [Handler1, Handler2, Handler3]
    readonly Dictionary<Type, List<Type>> extensionPoints = [];

    // Storage: handler type → list of message types it handles
    // Example: OrderHandler → [OrderCreated, OrderCancelled]
    readonly Dictionary<Type, List<Type>> handlerToMessageTypes = [];

    /// <summary>
    /// Registers an implementation type for a given extension point marker.
    /// Called by source-generated code.
    /// </summary>
    /// <typeparam name="TExtension">The extension point marker type (e.g., IHandleMessages, Feature)</typeparam>
    /// <typeparam name="TImplementation">The implementation type (e.g., MyHandler, MyFeature)</typeparam>
    public void RegisterExtensionType<TExtension, TImplementation>()
        where TImplementation : TExtension
    {
        var markerType = typeof(TExtension);

        if (!extensionPoints.TryGetValue(markerType, out var types))
        {
            types = [];
            extensionPoints[markerType] = types;
        }

        if (!types.Contains(typeof(TImplementation)))
        {
            types.Add(typeof(TImplementation));
        }
    }

    /// <summary>
    /// Gets all implementation types registered for a specific marker type.
    /// Used internally by NServiceBus to retrieve handlers, features, etc.
    /// </summary>
    /// <param name="markerType">The extension point marker type</param>
    /// <returns>All registered implementation types for the marker</returns>
    internal IEnumerable<Type> GetAvailableTypes(Type markerType)
    {
        return extensionPoints.TryGetValue(markerType, out var types)
            ? types
            : Enumerable.Empty<Type>();
    }

    /// <summary>
    /// Registers a handler with its specific message type.
    /// Used by source-generated code for handlers.
    /// </summary>
    /// <typeparam name="THandler">The handler type</typeparam>
    /// <typeparam name="TMessage">The message type it handles</typeparam>
    public void RegisterHandler<THandler, TMessage>()
        where THandler : IHandleMessages<TMessage>
    {
        var handlerType = typeof(THandler);
        var messageType = typeof(TMessage);

        // Register as extension point
        RegisterExtensionType<IHandleMessages, THandler>();

        // Store handler → message mapping
        if (!handlerToMessageTypes.TryGetValue(handlerType, out var messageTypes))
        {
            messageTypes = [];
            handlerToMessageTypes[handlerType] = messageTypes;
        }

        if (!messageTypes.Contains(messageType))
        {
            messageTypes.Add(messageType);
        }
    }

    /// <summary>
    /// Gets all message types that a handler can handle.
    /// Used internally by MessageHandlerRegistry.
    /// </summary>
    /// <param name="handlerType">The handler type</param>
    /// <returns>All message types the handler can handle</returns>
    internal IEnumerable<Type> GetMessageTypesForHandler(Type handlerType)
    {
        return handlerToMessageTypes.TryGetValue(handlerType, out var messageTypes)
            ? messageTypes
            : Enumerable.Empty<Type>();
    }

    /// <summary>
    /// Gets all registered types across all extension points.
    /// Used as a replacement for assembly scanning results.
    /// </summary>
    /// <returns>All registered types</returns>
    internal IEnumerable<Type> GetAllRegisteredTypes()
    {
        return extensionPoints.Values.SelectMany(types => types).Distinct();
    }
}