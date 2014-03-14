namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;
    using System.ComponentModel;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MessageMetadataRegistry
    {
        public MessageMetadata GetMessageDefinition(Type messageType)
        {
            MessageMetadata metadata;
            if (messages.TryGetValue(messageType, out metadata))
            {
                return metadata;
            }
            var message = string.Format("Could not find Metadata for '{0}'.{1}Please ensure the following:{1}1. '{0}' is included in initial scanning see File Scanning: http://particular.net/articles/the-nservicebus-host{1}2. '{0}' implements either 'IMessage', 'IEvent' or 'ICommand' or alternatively, if you don't want to implement an interface, you can use 'Unobtrusive Mode' see: http://particular.net/articles/unobtrusive-mode-messages", messageType.FullName, Environment.NewLine);
            throw new Exception(message);
        }

        public bool HasDefinitionFor(Type messageType)
        {
            return messages.ContainsKey(messageType);
        }


        [ObsoleteEx(RemoveInVersion = "5.0")]
        public IEnumerable<MessageMetadata> GetMessageTypes(TransportMessage message)
        {
            string header;

            if (!message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out header))
            {
                yield break;
            }
            foreach (var messageTypeString in header.Split(';'))
            {
                yield return GetMessageMetadata(messageTypeString);
            }
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
            else
            {
                MessageMetadata metadata;
                if (messages.TryGetValue(messageType, out metadata))
                {
                    return metadata;
                }
                Logger.WarnFormat("Message header '{0}' was mapped to type '{1}' but that type was not found in the message registry, please make sure the same message registration conventions are used in all endpoints, specially if you are using unobtrusive mode. ", messageType, messageType.FullName);
            }
            return null;
        }

        public IEnumerable<MessageMetadata> GetAllMessages()
        {
            return new List<MessageMetadata>(messages.Values);
        }


        public void RegisterMessageType(Type messageType)
        {
            var metadata = new MessageMetadata
                           {
                               MessageType = messageType,
                               Recoverable = !DefaultToNonPersistentMessages,
                               TimeToBeReceived = MessageConventionExtensions.TimeToBeReceivedAction(messageType)
                           };

            if (MessageConventionExtensions.IsExpressMessageType(messageType))
                metadata.Recoverable = false;

            //get the parent types
            var parentMessages = GetParentTypes(messageType)
                .Where(MessageConventionExtensions.IsMessageType)
                .OrderByDescending(PlaceInMessageHierarchy)
                .ToList();

            metadata.MessageHierarchy = new[] { messageType }.Concat(parentMessages);

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

        readonly Dictionary<Type, MessageMetadata> messages = new Dictionary<Type, MessageMetadata>();

        public bool DefaultToNonPersistentMessages { get; set; }

        static ILog Logger = LogManager.GetLogger(typeof(DefaultDispatcherFactory));
    }
}