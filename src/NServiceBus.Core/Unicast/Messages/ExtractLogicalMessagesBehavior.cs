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

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ExtractLogicalMessagesBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        IMessageSerializer messageSerializer;
        UnicastBus unicastBus;
        LogicalMessageFactory logicalMessageFactory;
        PipelineFactory pipelineFactory;
        MessageMetadataRegistry messageMetadataRegistry;

        internal ExtractLogicalMessagesBehavior(IMessageSerializer messageSerializer, UnicastBus unicastBus, LogicalMessageFactory logicalMessageFactory, PipelineFactory pipelineFactory, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.messageSerializer = messageSerializer;
            this.unicastBus = unicastBus;
            this.logicalMessageFactory = logicalMessageFactory;
            this.pipelineFactory = pipelineFactory;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        internal bool SkipDeserialization { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            if (context.MessageHandlingDisabled)
            {
                return;
            }

            if (SkipDeserialization || unicastBus.SkipDeserialization)
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
                pipelineFactory.InvokeLogicalMessagePipeline(message);
            }

            if (!messages.Any())
            {
                log.Warn("Received an empty message - ignoring.");
            }

            next();
        }

        IEnumerable<LogicalMessage> Extract(TransportMessage physicalMessage)
        {
            if (physicalMessage.Body == null || physicalMessage.Body.Length == 0)
            {
                return new List<LogicalMessage>();
            }

            var messageMetadata = messageMetadataRegistry.GetMessageTypes(physicalMessage);

            using (var stream = new MemoryStream(physicalMessage.Body))
            {
                var messageTypesToDeserialize = messageMetadata.Select(metadata => metadata.MessageType).ToList();

                return messageSerializer.Deserialize(stream, messageTypesToDeserialize).Select(rawMessage => 
                    logicalMessageFactory.Create(rawMessage.GetType(),rawMessage,physicalMessage.Headers))
                    .ToList();
            }

        }

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}