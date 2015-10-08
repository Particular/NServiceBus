namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Scheduling.Messages;
    using Serializers;
    using Transports;
    using Unicast.Messages;

    class DeserializeLogicalMessagesConnector : StageConnector<PhysicalMessageProcessingContext, LogicalMessageProcessingContext>
    {
        public MessageDeserializerResolver DeserializerResolver { get; set; }

        public LogicalMessageFactory LogicalMessageFactory { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<LogicalMessageProcessingContext, Task> next)
        {
            var incomingMessage = context.Message;

            var messages = ExtractWithExceptionHandling(incomingMessage);

            foreach (var message in messages)
            {
                await next(new LogicalMessageProcessingContext(message, context.Message.Headers, context)).ConfigureAwait(false);
            }
        }

        List<LogicalMessage> ExtractWithExceptionHandling(IncomingMessage message)
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

        List<LogicalMessage> Extract(IncomingMessage physicalMessage)
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
                    var typeString = messageTypeString;

                    if (DoesTypeHaveImplAddedByVersion3(typeString))
                    {
                        continue;
                    }

                    MessageMetadata metadata;

                    if (IsV4OrBelowScheduledTask(typeString))
                    {
                        metadata = MessageMetadataRegistry.GetMessageMetadata(typeof(ScheduledTask));
                    }
                    else
                    {
                        metadata = MessageMetadataRegistry.GetMessageMetadata(typeString);
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

            using (var stream = new MemoryStream(physicalMessage.Body))
            {
                var messageTypes = messageMetadata.Select(metadata => metadata.MessageType).ToList();
                var messageSerializer = DeserializerResolver.Resolve(physicalMessage.Headers[Headers.ContentType]);
                return messageSerializer.Deserialize(stream, messageTypes)
                    .Select(x => LogicalMessageFactory.Create(x.GetType(), x))
                    .ToList();
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

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}