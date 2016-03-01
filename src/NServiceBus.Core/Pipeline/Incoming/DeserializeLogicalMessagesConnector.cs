namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;

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
            var messages = ExtractWithExceptionHandling(context);

            foreach (var message in messages)
            {
                await stage(this.CreateIncomingLogicalMessageContext(message, context)).ConfigureAwait(false);
            }
        }

        List<LogicalMessage> ExtractWithExceptionHandling(IIncomingPhysicalMessageContext context)
        {
            try
            {
                return Extract(context);
            }
            catch (Exception exception)
            {
                throw new MessageDeserializationException(context.MessageId, exception);
            }
        }

        List<LogicalMessage> Extract(IIncomingPhysicalMessageContext context)
        {
            if (context.Body == null || context.Body.Length == 0)
            {
                return new List<LogicalMessage>();
            }

            string messageTypeIdentifier;
            var messageMetadata = new List<MessageMetadata>();

            if (context.Headers.TryGetValue(Headers.EnclosedMessageTypes, out messageTypeIdentifier))
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

                if (messageMetadata.Count == 0 && context.GetMesssageIntent() != MessageIntentEnum.Publish)
                {
                    log.WarnFormat("Could not determine message type from message header '{0}'. MessageId: {1}", messageTypeIdentifier, context.MessageId);
                }
            }

            var messageTypes = messageMetadata.Select(metadata => metadata.MessageType).ToList();
            var messageSerializer = deserializerResolver.Resolve(context.Headers);

            // For nested behaviors who have an expectation ContentType existing 
            // add the default content type 
            context.Headers[Headers.ContentType] = messageSerializer.ContentType;

            using (var stream = new MemoryStream(context.Body))
            {
                return messageSerializer.Deserialize(stream, messageTypes)
                    .Select(x => logicalMessageFactory.Create(x.GetType(), x))
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

        MessageDeserializerResolver deserializerResolver;
        LogicalMessageFactory logicalMessageFactory;
        MessageMetadataRegistry messageMetadataRegistry;

        static ILog log = LogManager.GetLogger<DeserializeLogicalMessagesConnector>();
    }
}