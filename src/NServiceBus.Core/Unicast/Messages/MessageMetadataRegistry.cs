namespace NServiceBus.Unicast.Messages;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Logging;
using NServiceBus;

/// <summary>
/// Cache of message metadata.
/// </summary>
public class MessageMetadataRegistry
{
    /// <summary>
    /// Create a new <see cref="MessageMetadataRegistry"/> instance.
    /// </summary>
    /// <param name="isMessageType">The function delegate indicating whether a specific type is a message type.</param>
    /// <param name="allowDynamicTypeLoading">When set to <c>true</c> the metadata registry will attempt to dynamically
    /// load types by using <see cref="Type.GetType(string)"/>; otherwise no attempts will be made to load types
    /// at runtime and all types must be explicitly loaded beforehand.</param>
    public MessageMetadataRegistry(Func<Type, bool> isMessageType, bool allowDynamicTypeLoading)
    {
        this.isMessageType = isMessageType;
        this.allowDynamicTypeLoading = allowDynamicTypeLoading;
    }

    /// <summary>
    /// Retrieves the <see cref="MessageMetadata" /> for the specified type.
    /// </summary>
    /// <param name="messageType">The message type to retrieve metadata for.</param>
    /// <returns>The <see cref="MessageMetadata" /> for the specified type.</returns>
    public MessageMetadata GetMessageMetadata(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        if (messages.TryGetValue(messageType.TypeHandle, out var metadata))
        {
            return metadata;
        }

        if (isMessageType(messageType))
        {
            return RegisterMessageType(messageType);
        }

        var message = $"Could not find metadata for '{messageType.FullName}'.{Environment.NewLine}Ensure the following:{Environment.NewLine}1. '{messageType.FullName}' is included in initial scanning. {Environment.NewLine}2. '{messageType.FullName}' implements either 'IMessage', 'IEvent' or 'ICommand' or alternatively, if you don't want to implement an interface, you can use 'Unobtrusive Mode'.";
        throw new Exception(message);
    }

    /// <summary>
    /// Retrieves the <see cref="MessageMetadata" /> for the message identifier.
    /// </summary>
    /// <param name="messageTypeIdentifier">The message identifier to retrieve metadata for.</param>
    /// <returns>The <see cref="MessageMetadata" /> for the specified type.</returns>
    public MessageMetadata GetMessageMetadata(string messageTypeIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageTypeIdentifier);

        var cacheHit = cachedTypes.TryGetValue(messageTypeIdentifier, out var messageType);

        if (!cacheHit)
        {
            messageType = GetType(messageTypeIdentifier);

            if (messageType == null)
            {
                foreach (var item in messages.Values)
                {
                    var messageTypeFullName = AssemblyQualifiedNameParser.GetMessageTypeNameWithoutAssembly(messageTypeIdentifier);

                    if (item.MessageType.FullName == messageTypeIdentifier ||
                        item.MessageType.FullName == messageTypeFullName)
                    {
                        Logger.DebugFormat("Message type: '{0}' was mapped to '{1}'", messageTypeIdentifier, item.MessageType.AssemblyQualifiedName);

                        cachedTypes[messageTypeIdentifier] = item.MessageType;
                        return item;
                    }
                }
                Logger.DebugFormat("Message type: '{0}' No match on known messages", messageTypeIdentifier);
            }

            cachedTypes[messageTypeIdentifier] = messageType;
        }

        if (messageType == null)
        {
            return null;
        }

        if (messages.TryGetValue(messageType.TypeHandle, out var metadata))
        {
            return metadata;
        }

        if (isMessageType(messageType))
        {
            return RegisterMessageType(messageType);
        }

        Logger.WarnFormat("Message header '{0}' was mapped to type '{1}' but that type was not found in the message registry, ensure the same message registration conventions are used in all endpoints, especially if using unobtrusive mode. ", messageType, messageType.FullName);
        return null;
    }

    /// <summary>
    /// Retrieves all known messages <see cref="MessageMetadata" />.
    /// </summary>
    /// <returns>An array of <see cref="MessageMetadata" /> for all known message.</returns>
    public MessageMetadata[] GetAllMessages() => messages.Values.ToArray();

    Type GetType(string messageTypeIdentifier)
    {
        if (allowDynamicTypeLoading)
        {
            try
            {
                return Type.GetType(messageTypeIdentifier);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Message type identifier '{messageTypeIdentifier}' could not be loaded", ex);
            }
        }
        else
        {
            Logger.Warn($"Unknown message type identifier '{messageTypeIdentifier}'. Dynamic type loading is disabled. Make sure the type is loaded before starting the endpoint or enable dynamic type loading.");
        }

        return null;
    }

    internal void RegisterMessageTypesFoundIn(IList<Type> availableTypes)
    {
        foreach (var type in availableTypes)
        {
            if (isMessageType(type))
            {
                RegisterMessageType(type);
                continue;
            }

            foreach (var messageType in GetHandledMessageTypes(type))
            {
                RegisterMessageType(messageType);
            }
        }
    }

    static IEnumerable<Type> GetHandledMessageTypes(Type messageHandlerType)
    {
        if (messageHandlerType.IsAbstract || messageHandlerType.IsGenericTypeDefinition)
        {
            yield break;
        }

        foreach (var handlerInterface in messageHandlerType.GetInterfaces())
        {
            if (handlerInterface.IsGenericType && handlerInterface.GetGenericTypeDefinition() == IHandleMessagesType)
            {
                yield return handlerInterface.GetGenericArguments()[0];
            }
        }
    }

    MessageMetadata RegisterMessageType(Type messageType)
    {
        if (messageType.IsGenericType)
        {
            // This is not an error because in most cases it will work, but it's still not supported should issues arise
            Logger.Warn($"Generic messages types are not supported. Consider converting {messageType.AssemblyQualifiedName} to a dedicated, simple type");
        }

        //get the parent types
        var parentMessages = GetParentTypes(messageType)
            .Where(t => isMessageType(t))
            .OrderByDescending(PlaceInMessageHierarchy);

        var metadata = new MessageMetadata(messageType, new[]
        {
                messageType
            }.Concat(parentMessages).ToArray());

        messages[messageType.TypeHandle] = metadata;
        cachedTypes.TryAdd(messageType.AssemblyQualifiedName, messageType);

        return metadata;
    }

    static int PlaceInMessageHierarchy(Type type)
    {
        if (type.IsInterface)
        {
            return type.GetInterfaces().Length;
        }

        var result = 0;

        while (type.BaseType != null)
        {
            result++;

            type = type.BaseType;
        }

        return result;
    }

    static IEnumerable<Type> GetParentTypes(Type type)
    {
        foreach (var i in type.GetInterfaces())
        {
            yield return i;
        }

        // return all inherited types
        var currentBaseType = type.BaseType;
        var objectType = typeof(object);
        while (currentBaseType != null && currentBaseType != objectType)
        {
            yield return currentBaseType;
            currentBaseType = currentBaseType.BaseType;
        }
    }

    readonly Func<Type, bool> isMessageType;
    readonly bool allowDynamicTypeLoading;
    readonly ConcurrentDictionary<RuntimeTypeHandle, MessageMetadata> messages = new ConcurrentDictionary<RuntimeTypeHandle, MessageMetadata>();
    readonly ConcurrentDictionary<string, Type> cachedTypes = new ConcurrentDictionary<string, Type>();

    static readonly Type IHandleMessagesType = typeof(IHandleMessages<>);
    static readonly ILog Logger = LogManager.GetLogger<MessageMetadataRegistry>();
}