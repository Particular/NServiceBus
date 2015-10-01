namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Scheduling.Messages;
    using NServiceBus.Serializers;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;

    class DeserializeLogicalMessagesConnector : StageConnector<PhysicalMessageProcessingStageBehavior.Context, LogicalMessageProcessingStageBehavior.Context>
    {
        public MessageDeserializerResolver DeserializerResolver { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public LogicalMessageFactory LogicalMessageFactory { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public async override Task Invoke(PhysicalMessageProcessingStageBehavior.Context context, Func<LogicalMessageProcessingStageBehavior.Context, Task> next)
        {
            var transportMessage = context.Message;

            var messages = ExtractWithExceptionHandling(transportMessage);

            foreach (var message in messages)
            {
                await next(new LogicalMessageProcessingStageBehavior.Context(message, context.Message.Headers, context)).ConfigureAwait(false);
            }

        }

        List<LogicalMessage> ExtractWithExceptionHandling(IncomingMessage transportMessage)
        {
            try
            {
                return Extract(transportMessage);
            }
            catch (Exception exception)
            {
                throw new MessageDeserializationException(transportMessage.MessageId, exception);
            }
        }

        List<LogicalMessage> Extract(IncomingMessage physicalMessage)
        {
            if (physicalMessage.BodyStream == null || physicalMessage.BodyStream.Length == 0)
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

            // TODO: Should we rewind or the serializer?
            var messageTypes = messageMetadata.Select(metadata => metadata.MessageType).ToList();
            var messageSerializer = DeserializerResolver.Resolve(physicalMessage.Headers[Headers.ContentType]);
            return messageSerializer.Deserialize(physicalMessage.BodyStream, messageTypes)
                .Select(x => LogicalMessageFactory.Create(x.GetType(), x))
                .ToList();
        }

        [ObsoleteEx(RemoveInVersion = "7.0")]
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