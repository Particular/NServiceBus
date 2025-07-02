#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logging;
using MessageInterfaces;
using Pipeline;
using Transport;
using Unicast.Messages;

class DeserializeMessageConnector(
    MessageDeserializerResolver deserializerResolver,
    LogicalMessageFactory logicalMessageFactory,
    MessageMetadataRegistry messageMetadataRegistry,
    IMessageMapper mapper,
    bool allowContentTypeInference)
    : StageConnector<IIncomingPhysicalMessageContext, IIncomingLogicalMessageContext>
{
    public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> stage)
    {
        var incomingMessage = context.Message;

        var messages = ExtractWithExceptionHandling(incomingMessage);

        bool first = true;
        foreach (var message in messages)
        {
            if (first) // ignore the legacy case in which a single message payload contained multiple messages
            {
                var availableMetricTags = context.Extensions.Get<IncomingPipelineMetricTags>();
                availableMetricTags.Add(MeterTags.MessageType, message.MessageType.FullName!);
                first = false;
            }
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
            return [];
        }

        if (physicalMessage.Body.Length == 0)
        {
            log.Debug("Received a message without body. Skipping deserialization.");
            return [];
        }

        List<Type>? messageTypes = null;
        if (physicalMessage.Headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageTypesValue))
        {
            messageTypes = enclosedMessageTypesStringToMessageTypes.GetOrAdd(enclosedMessageTypesValue,
                static (key, registry) =>
                {
                    var keySpan = key.AsSpan();
                    var types = new List<Type>();

                    foreach (var messageTypeRange in keySpan.Split(EnclosedMessageTypeSeparator))
                    {
                        ReadOnlySpan<char> messageTypeSpan = keySpan[messageTypeRange].Trim();
                        if (DoesTypeHaveImplAddedByVersion3(messageTypeSpan))
                        {
                            continue;
                        }

                        var metadata = registry.GetMessageMetadata(messageTypeSpan.ToString());

                        if (metadata != null)
                        {
                            types.Add(metadata.MessageType);
                        }
                    }

                    return types;
                }, messageMetadataRegistry);

            if (messageTypes is { Count: 0 } && allowContentTypeInference && physicalMessage.GetMessageIntent() != MessageIntent.Publish)
            {
                log.WarnFormat("Could not determine message type from message header '{0}'. MessageId: {1}", enclosedMessageTypesValue, physicalMessage.MessageId);
            }
        }

        if (messageTypes is null or { Count: 0 } && !allowContentTypeInference)
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

    static bool DoesTypeHaveImplAddedByVersion3(ReadOnlySpan<char> existingTypeString) => existingTypeString.IndexOf(ImplSuffix) != -1;

    readonly ConcurrentDictionary<string, List<Type>> enclosedMessageTypesStringToMessageTypes = new();

    static readonly ILog log = LogManager.GetLogger<DeserializeMessageConnector>();
    static ReadOnlySpan<char> ImplSuffix => "__impl".AsSpan();
    static ReadOnlySpan<char> EnclosedMessageTypeSeparator => ";".AsSpan();
}