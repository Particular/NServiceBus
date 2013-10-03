namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Pipeline;
    using NServiceBus.Serialization;

    public class MessageExtractor : IBehavior
    {
        readonly IMessageSerializer messageSerializer;
        readonly MessageMetadataRegistry messageMetadataRegistry;
        public IBehavior Next { get; set; }

        public MessageExtractor(IMessageSerializer messageSerializer, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.messageSerializer = messageSerializer;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public bool SkipDeserialization { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            PerformInvocation(context);
            
            Next.Invoke(context);
        }

        void PerformInvocation(IBehaviorContext context)
        {
            if (SkipDeserialization)
            {
                context.Set(new object[0]);
                return;
            }

            var transportMessage = context.Get<TransportMessage>();
            try
            {
                var logicalMessages = Extract(transportMessage);

                context.Set(logicalMessages);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(
                    string.Format(
                        "An error occurred while attempting to extract logical messages from transport message {0}",
                        transportMessage), exception);
            }
        }

        object[] Extract(TransportMessage m)
        {

            if (m.Body == null || m.Body.Length == 0)
            {
                return null;
            }

            try
            {

                var messageMetadata = messageMetadataRegistry.GetMessageTypes(m);

                using (var stream = new MemoryStream(m.Body))
                {
                    return messageSerializer.Deserialize(stream, messageMetadata.Select(metadata => metadata.MessageType).ToList());
                }
            }
            catch (Exception e)
            {
                throw new SerializationException("Could not deserialize message.", e);
            }
        }

    }
}