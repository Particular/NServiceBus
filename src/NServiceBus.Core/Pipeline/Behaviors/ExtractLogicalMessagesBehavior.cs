namespace NServiceBus.Pipeline.Behaviors
{
    using System;
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

    class ExtractLogicalMessagesBehavior : IBehavior<PhysicalMessageContext>
    {
        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IMessageSerializer MessageSerializer { get; set; }

        public IMessageMapper MessageMapper { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public bool SkipDeserialization { get; set; }

        public PipelineFactory PipelineFactory { get; set; }

        public void Invoke(PhysicalMessageContext context, Action next)
        {
            var logicalMessages = new LogicalMessages();

            context.Set(logicalMessages);

            if (SkipDeserialization || UnicastBus.SkipDeserialization)
            {
                return;
            }

            var transportMessage = context.PhysicalMessage;

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

                var logicalMessage = new LogicalMessage(messageType, rawMessage);

                logicalMessages.Add(logicalMessage);

                PipelineFactory.InvokeLogicalMessagePipeline(logicalMessage);
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
}