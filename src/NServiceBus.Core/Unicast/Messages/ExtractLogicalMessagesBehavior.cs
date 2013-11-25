namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
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

    class ExtractLogicalMessagesBehavior : IBehavior<PhysicalMessageContext>
    {

        public IMessageSerializer MessageSerializer { get; set; }
    
        public UnicastBus UnicastBus { get; set; }

        public LogicalMessageFactory LogicalMessageFactory { get; set; }

        public bool SkipDeserialization { get; set; }

        public PipelineFactory PipelineFactory { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public void Invoke(PhysicalMessageContext context, Action next)
        {
            if (SkipDeserialization || UnicastBus.SkipDeserialization)
            {
                next();
                return;
            }

            var transportMessage = context.PhysicalMessage;

            IEnumerable<LogicalMessage> messages;
            
            if (transportMessage.IsControlMessage())
            {
                log.Info("Received a control message. Skipping deserialization as control message data is contained in the header.");
                next();
                return;
            }

            try
            {
                messages = Extract(transportMessage).ToList();
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("An error occurred while attempting to extract logical messages from transport message {0}", transportMessage), exception);
            }

          
            foreach (var message in messages)
            {
                PipelineFactory.InvokeLogicalMessagePipeline(message);
            }

            if (!messages.Any())
            {
                log.Warn("Received an empty message - ignoring.");
            }

            next();
        }

        IEnumerable<LogicalMessage> Extract(TransportMessage m)
        {
            if (m.Body == null || m.Body.Length == 0)
            {
                return new List<LogicalMessage>();
            }

            var messageMetadata = MessageMetadataRegistry.GetMessageTypes(m);

            using (var stream = new MemoryStream(m.Body))
            {
                var messageTypesToDeserialize = messageMetadata.Select(metadata => metadata.MessageType).ToList();

                return MessageSerializer.Deserialize(stream, messageTypesToDeserialize).Select(rawMessage => LogicalMessageFactory.Create(rawMessage.GetType(),rawMessage)).ToList();
            }

        }

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    }
}