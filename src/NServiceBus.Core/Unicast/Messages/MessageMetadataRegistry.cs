namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    ///     Cache of message metadata.
    /// </summary>
    public class MessageMetadataRegistry
    {
        internal MessageMetadataRegistry(bool defaultToNonPersistentMessages, Conventions conventions)
        {
            this.defaultToNonPersistentMessages = defaultToNonPersistentMessages;
            this.conventions = conventions;
        }

        internal bool DefaultToNonPersistentMessages
        {
            get { return defaultToNonPersistentMessages; }
            set { defaultToNonPersistentMessages = value; }
        }

        /// <summary>
        ///     Retrieves the <see cref="MessageMetadata" /> for the specified type.
        /// </summary>
        /// <param name="messageType">The message type to retrieve metadata for.</param>
        /// <returns>The <see cref="MessageMetadata" /> for the specified type.</returns>
        public MessageMetadata GetMessageMetadata(Type messageType)
        {
            Guard.AgainstNull(messageType, "messageType");
            MessageMetadata metadata;
            if (messages.TryGetValue(messageType.TypeHandle, out metadata))
            {
                return metadata;
            }

            if (conventions.IsMessageType(messageType))
            {
                return RegisterMessageType(messageType);
            }

            var message = string.Format("Could not find metadata for '{0}'.{1}Please ensure the following:{1}1. '{0}' is included in initial scanning. {1}2. '{0}' implements either 'IMessage', 'IEvent' or 'ICommand' or alternatively, if you don't want to implement an interface, you can use 'Unobtrusive Mode'.", messageType.FullName, Environment.NewLine);
            throw new Exception(message);
        }

        /// <summary>
        ///     Retrieves the <see cref="MessageMetadata" /> for the message identifier.
        /// </summary>
        /// <param name="messageTypeIdentifier">The message identifier to retrieve metadata for.</param>
        /// <returns>The <see cref="MessageMetadata" /> for the specified type.</returns>
        public MessageMetadata GetMessageMetadata(string messageTypeIdentifier)
        {
            Guard.AgainstNullAndEmpty(messageTypeIdentifier, "messageTypeIdentifier");

            var messageType = Type.GetType(messageTypeIdentifier, false);

            if (messageType == null)
            {
                Logger.DebugFormat("Message type: '{0}' could not be determined by a 'Type.GetType', scanning known messages for a match", messageTypeIdentifier);
                return messages.Values.FirstOrDefault(m => m.MessageType.FullName == messageTypeIdentifier);
            }
            MessageMetadata metadata;
            if (messages.TryGetValue(messageType.TypeHandle, out metadata))
            {
                return metadata;
            }

            Logger.WarnFormat("Message header '{0}' was mapped to type '{1}' but that type was not found in the message registry, please make sure the same message registration conventions are used in all endpoints, specially if you are using unobtrusive mode. ", messageType, messageType.FullName);
            return null;
        }

        internal IEnumerable<MessageMetadata> GetAllMessages()
        {
            return new List<MessageMetadata>(messages.Values);
        }

        internal MessageMetadata RegisterMessageType(Type messageType)
        {
            //get the parent types
            var parentMessages = GetParentTypes(messageType)
                .Where(conventions.IsMessageType)
                .OrderByDescending(PlaceInMessageHierarchy)
                .ToList();

            var timeToBeReceived = conventions.GetTimeToBeReceived(messageType);
            var isExpressMessageType = conventions.IsExpressMessageType(messageType);
            var recoverable = !isExpressMessageType && !defaultToNonPersistentMessages;
            var metadata = new MessageMetadata(messageType, recoverable, timeToBeReceived, new[]
            {
                messageType
            }.Concat(parentMessages));

            messages[messageType.TypeHandle] = metadata;

            return metadata;
        }

        int PlaceInMessageHierarchy(Type type)
        {
            if (type.IsInterface)
            {
                return type.GetInterfaces().Count();
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
            var objectType = typeof(Object);
            while (currentBaseType != null && currentBaseType != objectType)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }

        static ILog Logger = LogManager.GetLogger<MessageMetadataRegistry>();
        readonly Conventions conventions;
        readonly Dictionary<RuntimeTypeHandle, MessageMetadata> messages = new Dictionary<RuntimeTypeHandle, MessageMetadata>();
        bool defaultToNonPersistentMessages;
    }
}
