namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Logging;
    using Unicast;
    using Unicast.Messages;
    using Pipeline;
    using Serialization;
    using Unicast.Transport;

    class ExtractLogicalMessagesBehavior : IBehavior
    {
        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IMessageSerializer MessageSerializer { get; set; }
        public UnicastBus UnicastBus { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public bool SkipDeserialization { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            if (SkipDeserialization || UnicastBus.SkipDeserialization)
            {
                context.Messages = new object[0];
                return;
            }

            var transportMessage = context.TransportMessage;

            try
            {
                context.Messages = Extract(transportMessage);
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("An error occurred while attempting to extract logical messages from transport message {0}", transportMessage), exception);
            }

            if (!transportMessage.IsControlMessage() && !context.Messages.Any())
            {
                context.Trace("Ignoring empty message with ID {0}", transportMessage.Id);
                log.Warn("Received an empty message - ignoring.");
                return;
            }

            next();
        }

        object[] Extract(TransportMessage m)
        {
            if (m.Body == null || m.Body.Length == 0)
            {
                return new object[0];
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