namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;

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