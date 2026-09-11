#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NServiceBus.Transport;

class ServicePlatformSender<TMessage>
{
    readonly Dictionary<string, string> headers;
    readonly JsonTypeInfo<TMessage> jsonTypeInfo;
    readonly IMessageDispatcher messageDispatcher;
    readonly UnicastAddressTag destinationAddress;

    public ServicePlatformSender(JsonTypeInfo<TMessage> jsonTypeInfo, IMessageDispatcher messageDispatcher, UnicastAddressTag destinationAddress, ReceiveAddresses? receiveAddresses)
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

        if (receiveAddresses is not null)
        {
            headers[Headers.ReplyToAddress] = receiveAddresses.MainReceiveAddress;
        }
    }

    public async Task Send(TMessage message, CancellationToken cancellationToken = default)
    {
        using var bufferWriter = new ArrayPoolBufferWriter<byte>();
        var writer = new Utf8JsonWriter(bufferWriter);
        await using var _ = writer.ConfigureAwait(false);
        JsonSerializer.Serialize(writer, message, jsonTypeInfo);

        var outgoingMessage = new OutgoingMessage(
            Guid.NewGuid().ToString(),
            headers,
            bufferWriter.WrittenMemory
        );

        var transportOperation = new TransportOperation(outgoingMessage, destinationAddress);
        await messageDispatcher.Dispatch(new(transportOperation), new(), cancellationToken).ConfigureAwait(false);
    }
}
