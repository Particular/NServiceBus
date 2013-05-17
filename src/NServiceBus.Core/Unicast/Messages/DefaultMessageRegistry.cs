namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    public class DefaultMessageRegistry : IMessageRegistry
    {
        public MessageMetadata GetMessageDefinition(Type messageType)
        {
            MessageMetadata metadata;
            if (messages.TryGetValue(messageType, out metadata))
            {
                return metadata;
            }
            var message = string.Format("Could not find Metadata for '{0}'. Messages need to implement either 'IMessage', 'IEvent' or 'ICommand'. Alternatively, if you don't want to implement an interface, you can configure 'Unobtrusive Mode Messages' and use convention to configure how messages are mapped.", messageType.FullName);
            throw new Exception(message);
        }

        public IEnumerable<MessageMetadata> GetMessageTypes(TransportMessage message)
        {
            IEnumerable<string> messageTypeStrings = null;
            if (message.Headers.ContainsKey(Headers.EnclosedMessageTypes))
            {
                var header = message.Headers[Headers.EnclosedMessageTypes];

                if (!string.IsNullOrEmpty(header))
                    messageTypeStrings = header.Split(';');
            }

            if (messageTypeStrings == null)
                yield break;

            foreach (var messageTypeString in messageTypeStrings)
            {
                var messageType = Type.GetType(messageTypeString, false);

                if (messageType == null)
                {
                    Logger.InfoFormat("Message type: {0} could not be determined by a Type.GetType, scanning known messages for a match", messageTypeString);

                    var firstOrDefault = messages.Values.FirstOrDefault(m => m.MessageType.FullName == messageTypeString);
                    if (firstOrDefault != null)
                    {
                        messageType = firstOrDefault.MessageType;
                    }
                }

                if (messageType == null)
                {
                    //we can look at doing more advanced lookups in the future
                    Logger.WarnFormat("Could not determine message type for message identifier: {0}",messageTypeString);
                }

                if (messageType != null)
                {
                    if (messages.ContainsKey(messageType))
                        yield return messages[messageType];
                    else
                    {
                        Logger.ErrorFormat("Asked for Message Metadata for a message of type {0}, which it has not been registered. If you are using unobtrusive mode, you need to register your types", messageType);
                    }
                }
            }
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
                    Recoverable = !DefaultToNonPersistentMessages
                };

            if (MessageConventionExtensions.IsExpressMessageType(messageType))
                metadata.Recoverable = false;

            metadata.TimeToBeReceived = MessageConventionExtensions.TimeToBeReceivedAction(messageType);

            messages[messageType] = metadata;
        }

        readonly IDictionary<Type, MessageMetadata> messages = new Dictionary<Type, MessageMetadata>();

        public bool DefaultToNonPersistentMessages { get; set; }

        static ILog Logger = LogManager.GetLogger(typeof (DefaultDispatcherFactory));
    }
}