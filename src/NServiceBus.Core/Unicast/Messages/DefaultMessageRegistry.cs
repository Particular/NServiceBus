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
            var messageMetadatas = new List<MessageMetadata>();
            string header;

            if (!message.Headers.TryGetValue(Headers.EnclosedMessageTypes, out header))
            {
                return messageMetadatas;
            }
            if (string.IsNullOrEmpty(header))
            {
                return messageMetadatas;
            }

            foreach (var messageTypeString in header.Split(';'))
            {
                var messageType = Type.GetType(messageTypeString, false);

                if (messageType == null)
                {
                    Logger.InfoFormat("Message type: '{0}' could not be determined by a 'Type.GetType', scanning known messages for a match.MessageId: {1}", messageTypeString, message.Id);

                    var messageMetadata = messages.Values.FirstOrDefault(m => m.MessageType.FullName == messageTypeString);
                    if (messageMetadata == null)
                    {
                        continue;
                    }
                    messageType = messageMetadata.MessageType;
                }
                MessageMetadata metadata;
                if (messages.TryGetValue(messageType, out metadata))
                {
                    messageMetadatas.Add(metadata);
                    continue;
                }
                Logger.WarnFormat("Message header '{0}' was mapped to type '{1}' but that type was not found in the message registry, check your type scanning conventions. MessageId: {2}", messageTypeString,messageType.FullName,message.Id);
            }

            if (messageMetadatas.Count == 0 && message.MessageIntent != MessageIntentEnum.Publish)
            {
                Logger.WarnFormat("Could not determine message type from message header '{0}'. MessageId: {1}", header, message.Id);
            }
            return messageMetadatas;
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

            messages[messageType] = metadata;
        }

        readonly Dictionary<Type, MessageMetadata> messages = new Dictionary<Type, MessageMetadata>();

        public bool DefaultToNonPersistentMessages { get; set; }

        static ILog Logger = LogManager.GetLogger(typeof (DefaultDispatcherFactory));
    }
}