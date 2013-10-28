namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Logging;
    using MessageInterfaces;
    using Unicast;
    using Unicast.Messages;
    using Pipeline;
    using Serialization;
    using Unicast.Transport;

    class ExtractLogicalMessagesBehavior : IBehavior
    {
        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IMessageSerializer MessageSerializer { get; set; }

        public IMessageMapper MessageMapper { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public bool SkipDeserialization { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            var logicalMessages = new LogicalMessages();

            context.Set(logicalMessages);

            if (SkipDeserialization || UnicastBus.SkipDeserialization)
            {
                return;
            }

            var transportMessage = context.TransportMessage;

            object[] rawMessages;

            try
            {
                rawMessages = Extract(transportMessage);
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("An error occurred while attempting to extract logical messages from transport message {0}", transportMessage), exception);
            }

            if (!transportMessage.IsControlMessage() && !rawMessages.Any())
            {
                log.Warn("Received an empty message - ignoring.");
                return;
            }

            foreach (var rawMessage in rawMessages)
            {
                var messageType = MessageMapper.GetMappedTypeFor(rawMessage.GetType());

                logicalMessages.Add(new Message(messageType,rawMessage));
            }

            next();
        }

        object[] Extract(TransportMessage m)
        {
            if (m.Body == null || m.Body.Length == 0)
            {
                return new object[0];
            }

            var messageMetadata = MessageMetadataRegistry.GetMessageTypes(m);

            using (var stream = new MemoryStream(m.Body))
            {
                return MessageSerializer.Deserialize(stream, messageMetadata.Select(metadata => metadata.MessageType).ToList());
            }

        }

    }
    internal class LogicalMessages : IEnumerable<Message>
    {
        public void Add(Message message)
        {
            messages.Add(message);
        }

        public IEnumerator<Message> GetEnumerator()
        {
            return messages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        List<Message> messages = new List<Message>();

    }

    internal class Message
    {
        public Message(Type messageType, object message)
        {
            Instance = message;
            MessageType = messageType;
        }

        public void UpdateMessageInstance(object newMessage)
        {
            Instance = newMessage;
        }

        public Type MessageType { get; private set; }
        public object Instance { get; private set; }
    }

}