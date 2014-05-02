namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Serialization;
    using Transport;
    using Unicast;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ExtractLogicalMessagesBehavior : IBehavior<ReceivePhysicalMessageContext>
    {

        public IMessageSerializer MessageSerializer { get; set; }
    
        public UnicastBus UnicastBus { get; set; }

        public LogicalMessageFactory LogicalMessageFactory { get; set; }

        public bool SkipDeserialization { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            if (SkipDeserialization)
            {
                next();
                return;
            }

            var transportMessage = context.PhysicalMessage;

            if (transportMessage.IsControlMessage())
            {
                log.Info("Received a control message. Skipping deserialization as control message data is contained in the header.");
                next();
                return;
            }

            try
            {
                context.LogicalMessages = Extract(transportMessage);
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("An error occurred while attempting to extract logical messages from transport message {0}", transportMessage), exception);
            }
            next();
        }

        List<LogicalMessage> Extract(TransportMessage physicalMessage)
        {
            if (physicalMessage.Body == null || physicalMessage.Body.Length == 0)
            {
                return new List<LogicalMessage>();
            }

            string messageTypeIdentifier;
            var messageMetadata = new List<MessageMetadata>();

            if (physicalMessage.Headers.TryGetValue(Headers.EnclosedMessageTypes, out messageTypeIdentifier))
            {
                foreach (var messageTypeString in messageTypeIdentifier.Split(';'))
                {
                    if (DoesTypeHaveImplAddedByVersion3(messageTypeString))
                    {
                        continue;
                    }

                    var metadata = MessageMetadataRegistry.GetMessageMetadata(messageTypeString);
                    if (metadata == null)
                    {
                        continue;
                    }
                    messageMetadata.Add(metadata);
                }

                if (messageMetadata.Count == 0 && physicalMessage.MessageIntent != MessageIntentEnum.Publish)
                {
                    log.WarnFormat("Could not determine message type from message header '{0}'. MessageId: {1}", messageTypeIdentifier, physicalMessage.Id);
                }
            }

            using (var stream = new MemoryStream(physicalMessage.Body))
            {
                var messageTypesToDeserialize = messageMetadata.Select(metadata => metadata.MessageType).ToList();

                return MessageSerializer.Deserialize(stream, messageTypesToDeserialize)
                    .Select(x => LogicalMessageFactory.Create(x.GetType(), x, physicalMessage.Headers))
                    .ToList();
            }
        }

        [ObsoleteEx(RemoveInVersion = "5.0")]
        bool DoesTypeHaveImplAddedByVersion3(string existingTypeString)
        {
            return existingTypeString.Contains("__impl");
        }

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}