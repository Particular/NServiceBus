namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using MessageInterfaces;
    using Pipeline;
    using Transport;
    using Unicast.Messages;

    class DeserializeMessageConnector : StageConnector<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>
    {
        public DeserializeMessageConnector(MessageDeserializerResolver deserializerResolver, LogicalMessageFactory logicalMessageFactory, MessageMetadataRegistry messageMetadataRegistry, IMessageMapper mapper)
        {
            this.deserializerResolver = deserializerResolver;
            this.logicalMessageFactory = logicalMessageFactory;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.mapper = mapper;
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
            incomingMessage.Headers.TryGetValue(Headers.ControlMessageHeader, out var value);
            return string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        LogicalMessage[] ExtractWithExceptionHandling(IncomingMessage message)
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

        LogicalMessage[] Extract(IncomingMessage physicalMessage)
        {
            // We need this check to be compatible with v3.3 endpoints, v3.3 control messages also include a body
            if (IsControlMessage(physicalMessage))
            {
                log.Debug("Received a control message. Skipping deserialization as control message data is contained in the header.");
                return NoMessagesFound;
            }

            if (physicalMessage.Body.Length == 0)
            {
                log.Debug("Received a message without body. Skipping deserialization.");
                return NoMessagesFound;
            }

            var messageMetadata = new List<MessageMetadata>();

            if (physicalMessage.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypeIdentifier))
            {
                foreach (var messageTypeString in messageTypeIdentifier.Split(EnclosedMessageTypeSeparator))
                {
                    var typeString = messageTypeString;

                    if (DoesTypeHaveImplAddedByVersion3(typeString))
                    {
                        continue;
                    }

                    var metadata = messageMetadataRegistry.GetMessageMetadata(typeString);

                    if (metadata == null)
                    {
                        continue;
                    }

                    messageMetadata.Add(metadata);
                }

                if (messageMetadata.Count == 0 && physicalMessage.GetMessageIntent() != MessageIntentEnum.Publish)
                {
                    log.WarnFormat("Could not determine message type from message header '{0}'. MessageId: {1}", messageTypeIdentifier, physicalMessage.MessageId);
                }
            }

            var messageTypes = messageMetadata.Select(metadata => metadata.MessageType).ToList();
            var messageSerializer = deserializerResolver.Resolve(physicalMessage.Headers);

            mapper.Initialize(messageTypes);

            // For nested behaviors who have an expectation ContentType existing
            // add the default content type
            physicalMessage.Headers[Headers.ContentType] = messageSerializer.ContentType;

            object[] deserializedMessages;
            using (var stream = new MemoryStream(physicalMessage.Body.ToArray()))
            {
                deserializedMessages = messageSerializer.Deserialize(stream, messageTypes);
            }

            var logicalMessages = new LogicalMessage[deserializedMessages.Length];
            for (var i = 0; i < deserializedMessages.Length; i++)
            {
                var x = deserializedMessages[i];
                logicalMessages[i] = logicalMessageFactory.Create(x.GetType(), x);
            }
            return logicalMessages;
        }

        static bool DoesTypeHaveImplAddedByVersion3(string existingTypeString)
        {
            return existingTypeString.Contains("__impl");
        }

        readonly MessageDeserializerResolver deserializerResolver;
        readonly LogicalMessageFactory logicalMessageFactory;
        readonly MessageMetadataRegistry messageMetadataRegistry;
        readonly IMessageMapper mapper;

        static readonly LogicalMessage[] NoMessagesFound = new LogicalMessage[0];

        static readonly char[] EnclosedMessageTypeSeparator =
        {
            ';'
        };

        static readonly ILog log = LogManager.GetLogger<DeserializeMessageConnector>();
    }
}