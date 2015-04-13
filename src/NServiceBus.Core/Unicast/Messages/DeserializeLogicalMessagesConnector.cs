namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;
    using Pipeline;
    using Pipeline.Contexts;
    using Scheduling.Messages;
    using Serialization;
    using Unicast;


    class DeserializeLogicalMessagesConnector : StageConnector<PhysicalMessageProcessingStageBehavior.Context, LogicalMessagesProcessingStageBehavior.Context>
    {
        public IMessageSerializer MessageSerializer { get; set; }
    
        public UnicastBus UnicastBus { get; set; }

        public LogicalMessageFactory LogicalMessageFactory { get; set; }

        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public override void Invoke(PhysicalMessageProcessingStageBehavior.Context context, Action<LogicalMessagesProcessingStageBehavior.Context> next)
        {
            var transportMessage = context.PhysicalMessage;

            if (transportMessage.IsControlMessage())
            {
                log.Info("Received a control message. Skipping deserialization as control message data is contained in the header.");
                next(new LogicalMessagesProcessingStageBehavior.Context(Enumerable.Empty<LogicalMessage>(), context));
                return;
            }
            var messages = ExtractWithExceptionHandling(transportMessage);
            next(new LogicalMessagesProcessingStageBehavior.Context(messages, context));
        }

        List<LogicalMessage> ExtractWithExceptionHandling(TransportMessage transportMessage)
        {
            try
            {
                return Extract(transportMessage);
            }
            catch (Exception exception)
            {
                throw new MessageDeserializationException(transportMessage.Id, exception);
            }
        }

        List<LogicalMessage> Extract(TransportMessage physicalMessage)
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

                if (messageMetadata.Count == 0 && physicalMessage.MessageIntent != MessageIntentEnum.Publish)
                {
                    log.WarnFormat("Could not determine message type from message header '{0}'. MessageId: {1}", messageTypeIdentifier, physicalMessage.Id);
                }
            }

            using (var stream = new MemoryStream(physicalMessage.Body))
            {
                var messageTypesToDeserialize = messageMetadata.Select(metadata => metadata.MessageType).ToList();
                return MessageSerializer.Deserialize(stream, messageTypesToDeserialize)
                    .Select(x => LogicalMessageFactory.Create(x.GetType(), x))
                    .ToList();

            }
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