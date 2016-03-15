namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Transports;
    using Unicast.Messages;

    class DeserializeLogicalMessagesConnector : StageConnector<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>
    {
        public DeserializeLogicalMessagesConnector(MessageDeserializerResolver deserializerResolver, LogicalMessageFactory logicalMessageFactory, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.deserializerResolver = deserializerResolver;
            this.logicalMessageFactory = logicalMessageFactory;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> stage)
        {
            var incomingMessage = context.Message;

            var messages = ExtractWithExceptionHandling(incomingMessage);

            foreach (var message in messages)
            {
                await stage(this.CreateIncomingLogicalMessageContext(message, context)).ConfigureAwait(false);
            }
        }

        static bool IsControlMessage(IncomingMessage incomingMessage)
        {
            return incomingMessage.Headers.ContainsKey(Headers.ControlMessageHeader) && incomingMessage.Headers[Headers.ControlMessageHeader] == Boolean.TrueString;
        }

        IEnumerable<LogicalMessage> ExtractWithExceptionHandling(IncomingMessage message)
        {
            try
            {
                return Extract(message);
            }
            catch (Exception exception)
            {
                throw new MessageDeserializationException(message.MessageId, exception);
            }
        }

        IEnumerable<LogicalMessage> Extract(IncomingMessage physicalMessage)
        {
            // We need this check to be compatible with v3.3 endpoints, v3.3 control messages also include a body
            if (IsControlMessage(physicalMessage))
            {
                log.Debug("Received a control message. Skipping deserialization as control message data is contained in the header.");
                return Enumerable.Empty<LogicalMessage>();
            }

            if (physicalMessage.Body == null || physicalMessage.Body.Length == 0)
            {
                log.Debug("Received a message without body. Skipping deserialization.");
                return Enumerable.Empty<LogicalMessage>();
            }

            string messageTypeIdentifier;
            var messageMetadata = new List<MessageMetadata>();

            if (physicalMessage.Headers.TryGetValue(Headers.EnclosedMessageTypes, out messageTypeIdentifier))
            {
                foreach (var messageTypeString in messageTypeIdentifier.Split(';'))
                {
                    var typeString = messageTypeString;

                    if (DoesTypeHaveImplAddedByVersion3(typeString))
                    {
                        continue;
                    }

                    MessageMetadata metadata;

                    if (IsV4OrBelowScheduledTask(typeString))
                    {
                        metadata = messageMetadataRegistry.GetMessageMetadata(typeof(ScheduledTask));
                    }
                    else
                    {
                        metadata = messageMetadataRegistry.GetMessageMetadata(typeString);
                    }

                    if (metadata == null)
                    {
                        continue;
                    }

                    messageMetadata.Add(metadata);
                }

                if (messageMetadata.Count == 0 && physicalMessage.GetMesssageIntent() != MessageIntentEnum.Publish)
                {
                    log.WarnFormat("Could not determine message type from message header '{0}'. MessageId: {1}", messageTypeIdentifier, physicalMessage.MessageId);
                }
            }

            var messageTypes = messageMetadata.Select(metadata => metadata.MessageType).ToList();
            var messageSerializer = deserializerResolver.Resolve(physicalMessage.Headers);

            // For nested behaviors who have an expectation ContentType existing 
            // add the default content type 
            physicalMessage.Headers[Headers.ContentType] = messageSerializer.ContentType;

            using (var stream = new MemoryStream(physicalMessage.Body))
            {
                return messageSerializer.Deserialize(stream, messageTypes)
                    .Select(x => logicalMessageFactory.Create(x.GetType(), x));
            }
        }

        bool DoesTypeHaveImplAddedByVersion3(string existingTypeString)
        {
            return existingTypeString.Contains("__impl");
        }

        bool IsV4OrBelowScheduledTask(string existingTypeString)
        {
            return existingTypeString.StartsWith("NServiceBus.Scheduling.Messages.ScheduledTask, NServiceBus.Core");
        }

        MessageDeserializerResolver deserializerResolver;
        LogicalMessageFactory logicalMessageFactory;
        MessageMetadataRegistry messageMetadataRegistry;

        static ILog log = LogManager.GetLogger<DeserializeLogicalMessagesConnector>();
    }
}