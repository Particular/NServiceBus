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
/// <remarks>
/// Create a new <see cref="MessageMetadataRegistry"/> instance.
/// </remarks>
public partial class MessageMetadataRegistry
{
    /// <summary>
    /// Creates a new instance of <see cref="MessageMetadataRegistry"/>.
    /// </summary>
    public MessageMetadataRegistry()
    {
    }

    /// <summary>
    /// Initializes the registry which makes it fully functional. When the registry is not initialized only the <see cref="RegisterMessageTypeWithHierarchy"/> and <see cref="RegisterMessageType"/> methods can be called.
    /// </summary>
    /// <param name="isMessageType">The function delegate indicating whether a specific type is a message type.</param>
    /// <param name="allowDynamicTypeLoading">When set to <c>true</c> the metadata registry will attempt to dynamically
    /// load types by using <see cref="Type.GetType(string)"/>; otherwise no attempts will be made to load types
    /// at runtime and all types must be explicitly loaded beforehand.</param>
    public void Initialize(Func<Type, bool> isMessageType, bool allowDynamicTypeLoading)
    {
        ArgumentNullException.ThrowIfNull(isMessageType);

        this.isMessageType = isMessageType;
        this.allowDynamicTypeLoading = allowDynamicTypeLoading;

        lock (preRegisteredMessagesWithHierarchy)
        {
            foreach (var (messageType, parentMessages) in preRegisteredMessagesWithHierarchy)
            {
                if (isMessageType(messageType))
                {
                    RegisterMessageTypeWithHierarchyCore(messageType, parentMessages);
                }
            }
            preRegisteredMessagesWithHierarchy.Clear();
        }

        lock (preRegisteredMessageTypes)
        {
            foreach (var messageType in preRegisteredMessageTypes)
            {
                if (isMessageType(messageType))
                {
                    _ = RegisterMessageTypeCore(messageType);
                }
            }
            preRegisteredMessageTypes.Clear();
        }

        initialized = true;
    }

    /// <summary>
    /// Retrieves the <see cref="MessageMetadata" /> for the specified type.
    /// </summary>
    /// <param name="messageType">The message type to retrieve metadata for.</param>
    /// <returns>The <see cref="MessageMetadata" /> for the specified type.</returns>
    public MessageMetadata GetMessageMetadata(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        AssertIsInitialized();

        if (messages.TryGetValue(messageType.TypeHandle, out var metadata))
        {
            return metadata;
        }

        if (isMessageType(messageType))
        {
            return RegisterMessageTypeCore(messageType);
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
        AssertIsInitialized();

        var cacheHit = cachedTypes.TryGetValue(messageTypeIdentifier, out var messageType);

        if (!cacheHit)
        {
            messageType = GetType(messageTypeIdentifier);

            if (messageType == null)
            {
                var messageTypeFullName = AssemblyQualifiedNameParser.GetMessageTypeNameWithoutAssembly(messageTypeIdentifier);
                foreach (var item in messages.Values)
                {
                    var typeFullName = item.MessageType.FullName.AsSpan();
                    if (typeFullName.SequenceEqual(messageTypeIdentifier) || typeFullName.SequenceEqual(messageTypeFullName))
                    {
                        if (Logger.IsDebugEnabled)
                        {
                            Logger.DebugFormat("Message type: '{0}' was mapped to '{1}'", messageTypeIdentifier, item.MessageType.AssemblyQualifiedName);
                        }

                        cachedTypes[messageTypeIdentifier] = item.MessageType;
                        return item;
                    }
                }

                if (Logger.IsDebugEnabled)
                {
                    Logger.DebugFormat("Message type: '{0}' No match on known messages", messageTypeIdentifier);
                }
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
            return RegisterMessageTypeCore(messageType);
        }

        Logger.WarnFormat("Message header '{0}' was mapped to type '{1}' but that type was not found in the message registry, ensure the same message registration conventions are used in all endpoints, especially if using unobtrusive mode. ", messageType, messageType.FullName);
        return null;
    }

    /// <summary>
    /// Retrieves all known messages <see cref="MessageMetadata" />.
    /// </summary>
    /// <returns>An array of <see cref="MessageMetadata" /> for all known message.</returns>
    public MessageMetadata[] GetAllMessages()
    {
        AssertIsInitialized();

        return [.. messages.Values];
    }

    /// <summary>
    /// Registers the potential message type.
    /// </summary>
    /// <param name="messageType">The potential message type that is checked against the convention when the registery is initialized.</param>
    public void RegisterMessageType(Type messageType)
    {
        if (!initialized)
        {
            lock (preRegisteredMessageTypes)
            {
                preRegisteredMessageTypes.Add(messageType);
            }
        }
        else
        {
            if (isMessageType(messageType))
            {
                _ = RegisterMessageTypeCore(messageType);
            }
        }
    }

    /// <summary>
    /// Registers the potential message type with the parent hierarchy.
    /// </summary>
    /// <param name="messageType">The potential message type that is checked against the convention when the registry is initialized.</param>
    /// <param name="parentMessages">The potential parent message hierarchy that is checked against the convention when the registry is initialized.</param>
    public void RegisterMessageTypeWithHierarchy(Type messageType, IEnumerable<Type> parentMessages)
    {
        if (!initialized)
        {
            lock (preRegisteredMessagesWithHierarchy)
            {
                preRegisteredMessagesWithHierarchy.Add((messageType, parentMessages));
            }
        }
        else
        {
            if (isMessageType(messageType))
            {
                RegisterMessageTypeWithHierarchyCore(messageType, parentMessages);
            }
        }
    }

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

    // Assumes the caller has already verified the types are message types
    internal void RegisterMessageTypes(IEnumerable<Type> messageTypes)
    {
        foreach (var messageType in messageTypes)
        {
            RegisterMessageTypeCore(messageType);
        }
    }

    void RegisterMessageTypeWithHierarchyCore(Type messageType, IEnumerable<Type> parentMessages)
    {
        LogGenericMessageTypeWarning(messageType);

        var metadata = new MessageMetadata(messageType, [messageType, .. parentMessages.Where(isMessageType)]);

        messages[messageType.TypeHandle] = metadata;
        cachedTypes.TryAdd(messageType.AssemblyQualifiedName, messageType);
    }

    MessageMetadata RegisterMessageTypeCore(Type messageType)
    {
        if (messages.TryGetValue(messageType.TypeHandle, out var metadata))
        {
            return metadata;
        }
    
        LogGenericMessageTypeWarning(messageType);

        //get the parent types
        var parentMessages = GetParentTypes(messageType)
            .Where(isMessageType)
            .OrderByDescending(PlaceInMessageHierarchy);

        metadata = new MessageMetadata(messageType, [messageType, .. parentMessages]);

        messages[messageType.TypeHandle] = metadata;
        cachedTypes.TryAdd(messageType.AssemblyQualifiedName, messageType);

        return metadata;
    }

    static void LogGenericMessageTypeWarning(Type messageType)
    {
        if (messageType.IsGenericType)
        {
            // This is not an error because in most cases it will work, but it's still not supported should issues arise
            Logger.Debug($"Generic messages types are not supported. Consider converting '{messageType.AssemblyQualifiedName}' to a dedicated, simple type");
        }
    }

    void AssertIsInitialized()
    {
        if (!initialized)
        {
            throw new InvalidOperationException("The message metadata registry is not initialized which is an indication the registry was attempted to be used before the endpoint was created which means not all conventions and settings are available yet and therefore the registry cannot give accurate information about message metadata.");
        }
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

    bool initialized;
    readonly List<(Type MessageType, IEnumerable<Type> Hierarchy)> preRegisteredMessagesWithHierarchy = [];
    readonly List<Type> preRegisteredMessageTypes = [];
    readonly ConcurrentDictionary<RuntimeTypeHandle, MessageMetadata> messages = new();
    readonly ConcurrentDictionary<string, Type> cachedTypes = new();
    Func<Type, bool> isMessageType;
    bool allowDynamicTypeLoading;

    static readonly ILog Logger = LogManager.GetLogger<MessageMetadataRegistry>();
}