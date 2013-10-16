namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using Unicast.Messages;
    using Pipeline;
    using Serialization;

    class ExtractLogicalMessagesBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public IMessageSerializer MessageSerializer { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

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
                context.Messages = new object[0];
                return;
            }

            var transportMessage = context.TransportMessage;
            try
            {
                var logicalMessages = Extract(transportMessage);

                context.Messages = logicalMessages;
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
                var messageMetadata = MessageMetadataRegistry.GetMessageTypes(m);

                using (var stream = new MemoryStream(m.Body))
                {
                    return MessageSerializer.Deserialize(stream, messageMetadata.Select(metadata => metadata.MessageType).ToList());
                }
            }
            catch (Exception e)
            {
                throw new SerializationException("Could not deserialize message.", e);
            }
        }

    }
}