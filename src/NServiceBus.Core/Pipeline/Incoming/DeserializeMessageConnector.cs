namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logging;
    using MessageInterfaces;
    using Pipeline;
    using Transport;
    using Unicast.Messages;

    class DeserializeMessageConnector : StageConnector<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>
    {
        public DeserializeMessageConnector(MessageDeserializerResolver deserializerResolver, LogicalMessageFactory logicalMessageFactory, MessageMetadataRegistry messageMetadataRegistry, IMessageMapper mapper, bool allowContentTypeInference)
        {
            this.deserializerResolver = deserializerResolver;
            this.logicalMessageFactory = logicalMessageFactory;
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.mapper = mapper;
            this.allowContentTypeInference = allowContentTypeInference;
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
                return Array.Empty<LogicalMessage>();
            }

            if (physicalMessage.Body.Length == 0)
            {
                log.Debug("Received a message without body. Skipping deserialization.");
                return Array.Empty<LogicalMessage>();
            }

            Type[] messageTypes = Array.Empty<Type>();
            if (physicalMessage.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var messageTypeIdentifier))
            {
                messageTypes = enclosedMessageTypesStringToMessageTypes.GetOrAdd(messageTypeIdentifier,
                    (key, registry) =>
                    {
                        string[] messageTypeStrings = key.Split(EnclosedMessageTypeSeparator);
                        var types = new List<Type>(messageTypeStrings.Length);
                        for (var index = 0; index < messageTypeStrings.Length; index++)
                        {
                            string messageTypeString = messageTypeStrings[index];
                            if (DoesTypeHaveImplAddedByVersion3(messageTypeString))
                            {
                                continue;
                            }

                            var metadata = registry.GetMessageMetadata(messageTypeString);

                            if (metadata == null)
                            {
                                continue;
                            }

                            types.Add(metadata.MessageType);
                        }

                        // using an array in order to be able to assign array empty as the default value
                        return types.ToArray();
                    }, messageMetadataRegistry);

                if (messageTypes.Length == 0 && allowContentTypeInference && physicalMessage.GetMessageIntent() != MessageIntent.Publish)
                {
                    log.WarnFormat("Could not determine message type from message header '{0}'. MessageId: {1}", messageTypeIdentifier, physicalMessage.MessageId);
                }
            }

            if (messageTypes.Length == 0 && !allowContentTypeInference)
            {
                throw new Exception($"Could not determine the message type from the '{Headers.EnclosedMessageTypes}' header and message type inference from the message body has been disabled. Ensure the header is set or enable message type inference.");
            }

            var messageSerializer = deserializerResolver.Resolve(physicalMessage.Headers);

            mapper.Initialize(messageTypes);

            // For nested behaviors who have an expectation ContentType existing
            // add the default content type
            physicalMessage.Headers[Headers.ContentType] = messageSerializer.ContentType;

            var deserializedMessages = messageSerializer.Deserialize(physicalMessage.Body, messageTypes);

            var logicalMessages = new LogicalMessage[deserializedMessages.Length];
            for (var i = 0; i < deserializedMessages.Length; i++)
            {
                var x = deserializedMessages[i];
                logicalMessages[i] = logicalMessageFactory.Create(x.GetType(), x);
            }
            return logicalMessages;
        }

        static bool DoesTypeHaveImplAddedByVersion3(string existingTypeString) => existingTypeString.AsSpan().IndexOf("__impl".AsSpan()) != -1;

        readonly MessageDeserializerResolver deserializerResolver;
        readonly LogicalMessageFactory logicalMessageFactory;
        readonly MessageMetadataRegistry messageMetadataRegistry;
        readonly IMessageMapper mapper;
        readonly bool allowContentTypeInference;

        readonly ConcurrentDictionary<string, Type[]> enclosedMessageTypesStringToMessageTypes =
            new ConcurrentDictionary<string, Type[]>();

        static readonly char[] EnclosedMessageTypeSeparator =
        {
            ';'
        };

        static readonly ILog log = LogManager.GetLogger<DeserializeMessageConnector>();
    }
}