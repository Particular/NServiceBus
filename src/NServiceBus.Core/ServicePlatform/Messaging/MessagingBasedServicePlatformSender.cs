#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NServiceBus.ServicePlatform;
using NServiceBus.Transport;

class MessagingBasedServicePlatformSender<TMessage> : ServicePlatformSender<TMessage>
{
    readonly Dictionary<string, string> headers;
    readonly JsonTypeInfo<TMessage> jsonTypeInfo;
    readonly IMessageDispatcher messageDispatcher;
    readonly UnicastAddressTag destinationAddress;

    public MessagingBasedServicePlatformSender(JsonTypeInfo<TMessage> jsonTypeInfo, IMessageDispatcher messageDispatcher, UnicastAddressTag destinationAddress, string? replyToAddress)
    {
        this.jsonTypeInfo = jsonTypeInfo;
        this.messageDispatcher = messageDispatcher;
        this.destinationAddress = destinationAddress;

        headers = new()
        {
            [Headers.EnclosedMessageTypes] = typeof(TMessage).FullName!,
            [Headers.ContentType] = ContentTypes.Json,
            [Headers.MessageIntent] = MessageIntent.Send.ToString()
        };

        if (replyToAddress is not null)
        {
            headers[Headers.ReplyToAddress] = replyToAddress;
        }
    }

    public override async Task Send(TMessage message, CancellationToken cancellationToken = default)
    {
        var transportOperation = CreateTransportOperation(message);
        await messageDispatcher.Dispatch(new(transportOperation), new(), cancellationToken).ConfigureAwait(false);
    }

    public override Task Send(TMessage message, IPipelineContext pipelineContext)
    {
        ArgumentNullException.ThrowIfNull(pipelineContext);

        if (!pipelineContext.Extensions.TryGet<PendingTransportOperations>(out var pendingOperations))
        {
            return Send(message, pipelineContext.CancellationToken);
        }

        pendingOperations.Add(CreateTransportOperation(message));
        return Task.CompletedTask;
    }

    internal TransportOperation CreateTransportOperation(TMessage message)
    {
        using var bufferWriter = new ArrayPoolBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            JsonSerializer.Serialize(writer, message, jsonTypeInfo);
        }

        // Pending operations outlive the pooled buffer, so the body is copied
        var outgoingMessage = new OutgoingMessage(
            Guid.NewGuid().ToString(),
            new Dictionary<string, string>(headers),
            bufferWriter.WrittenMemory.ToArray()
        );

        return new TransportOperation(outgoingMessage, destinationAddress);
    }
}
