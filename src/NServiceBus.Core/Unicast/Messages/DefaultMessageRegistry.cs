namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;

    public class DefaultMessageRegistry : IMessageRegistry
    {
        public MessageMetadata GetMessageDefinition(Type messageType)
        {
            return messages[messageType];
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
    }
}