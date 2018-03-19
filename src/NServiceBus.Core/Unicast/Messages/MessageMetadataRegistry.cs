namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    /// Cache of message metadata.
    /// </summary>
    public class MessageMetadataRegistry
    {
        internal MessageMetadataRegistry(Func<Type, bool> isMessageType)
        {
            this.isMessageType = isMessageType;
        }

        /// <summary>
        /// Retrieves the <see cref="MessageMetadata" /> for the specified type.
        /// </summary>
        /// <param name="messageType">The message type to retrieve metadata for.</param>
        /// <returns>The <see cref="MessageMetadata" /> for the specified type.</returns>
        public MessageMetadata GetMessageMetadata(Type messageType)
        {
            Guard.AgainstNull(nameof(messageType), messageType);

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
            Guard.AgainstNullAndEmpty(nameof(messageTypeIdentifier), messageTypeIdentifier);

            var messageType = GetType(messageTypeIdentifier);

            if (messageType == null)
            {
                Logger.DebugFormat("Message type: '{0}' could not be determined by a 'Type.GetType', scanning known messages for a match", messageTypeIdentifier);

                foreach (var item in messages.Values)
                {
                    if (item.MessageType.FullName == messageTypeIdentifier)
                    {
                        cachedTypes[messageTypeIdentifier] = item.MessageType;
                        return item;
                    }
                }

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

        Type GetType(string messageTypeIdentifier)
        {
            if (!cachedTypes.TryGetValue(messageTypeIdentifier, out var type))
            {
                type = Type.GetType(messageTypeIdentifier, false);
                cachedTypes[messageTypeIdentifier] = type;
            }

            return type;
        }

        internal IEnumerable<MessageMetadata> GetAllMessages()
        {
            return new List<MessageMetadata>(messages.Values);
        }

        internal void RegisterMessageTypesFoundIn(IList<Type> availableTypes)
        {
            foreach (var type in availableTypes)
            {
                if (isMessageType(type))
                {
                    RegisterMessageType(type);
                }

                foreach (var messageType in GetHandledTypes(type))
                {
                    RegisterMessageType(messageType);
                }
            }
        }

        IEnumerable<Type> GetHandledTypes(Type messageHandlerType)
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
            //get the parent types
            var parentMessages = GetParentTypes(messageType)
                .Where(t => isMessageType(t))
                .OrderByDescending(PlaceInMessageHierarchy);

            var metadata = new MessageMetadata(messageType, new[]
            {
                    messageType
                }.Concat(parentMessages).ToArray());

            messages[messageType.TypeHandle] = metadata;

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

        Func<Type, bool> isMessageType;
        ConcurrentDictionary<RuntimeTypeHandle, MessageMetadata> messages = new ConcurrentDictionary<RuntimeTypeHandle, MessageMetadata>();
        ConcurrentDictionary<string, Type> cachedTypes = new ConcurrentDictionary<string, Type>();

        static Type IHandleMessagesType = typeof(IHandleMessages<>);
        static ILog Logger = LogManager.GetLogger<MessageMetadataRegistry>();
    }
}