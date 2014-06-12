namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    class MessageMetadataRegistry
    {
        public MessageMetadataRegistry(bool defaultToNonPersistentMessages, Conventions conventions)
        {
            this.defaultToNonPersistentMessages = defaultToNonPersistentMessages;
            this.conventions = conventions;
        }

        public bool DefaultToNonPersistentMessages
        {
            get { return defaultToNonPersistentMessages; }
            set { defaultToNonPersistentMessages = value; }
        }

        public MessageMetadata GetMessageDefinition(Type messageType)
        {
            MessageMetadata metadata;
            if (messages.TryGetValue(messageType, out metadata))
            {
                return metadata;
            }
            var message = string.Format("Could not find metadata for '{0}'.{1}Please ensure the following:{1}1. '{0}' is included in initial scanning see File Scanning: http://particular.net/articles/the-nservicebus-host{1}2. '{0}' implements either 'IMessage', 'IEvent' or 'ICommand' or alternatively, if you don't want to implement an interface, you can use 'Unobtrusive Mode' see: http://particular.net/articles/unobtrusive-mode-messages", messageType.FullName, Environment.NewLine);
            throw new Exception(message);
        }

        public bool HasDefinitionFor(Type messageType)
        {
            return messages.ContainsKey(messageType);
        }

        public MessageMetadata GetMessageMetadata(string messageTypeIdentifier)
        {
            if (string.IsNullOrEmpty(messageTypeIdentifier))
            {
                throw new ArgumentException("MessageTypeIdentifier passed is null or empty");
            }

            var messageType = Type.GetType(messageTypeIdentifier, false);

            if (messageType == null)
            {
                Logger.DebugFormat("Message type: '{0}' could not be determined by a 'Type.GetType', scanning known messages for a match", messageTypeIdentifier);
                return messages.Values.FirstOrDefault(m => m.MessageType.FullName == messageTypeIdentifier);
            }
            MessageMetadata metadata;
            if (messages.TryGetValue(messageType, out metadata))
            {
                return metadata;
            }
            Logger.WarnFormat("Message header '{0}' was mapped to type '{1}' but that type was not found in the message registry, please make sure the same message registration conventions are used in all endpoints, specially if you are using unobtrusive mode. ", messageType, messageType.FullName);
            return null;
        }

        public IEnumerable<MessageMetadata> GetAllMessages()
        {
            return new List<MessageMetadata>(messages.Values);
        }


        public void RegisterMessageType(Type messageType)
        {
            //get the parent types
            var parentMessages = GetParentTypes(messageType)
                .Where(conventions.IsMessageType)
                .OrderByDescending(PlaceInMessageHierarchy)
                .ToList();

            var metadata = new MessageMetadata(messageType, !conventions.IsExpressMessageType(messageType) && !defaultToNonPersistentMessages, conventions.TimeToBeReceivedAction(messageType), new[]
            {
                messageType
            }.Concat(parentMessages));

            messages[messageType] = metadata;
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
        readonly Dictionary<Type, MessageMetadata> messages = new Dictionary<Type, MessageMetadata>();
        bool defaultToNonPersistentMessages;
    }
}