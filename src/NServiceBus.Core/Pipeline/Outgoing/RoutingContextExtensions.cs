#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using Pipeline;
using Routing;
using Transport;

static class RoutingContextExtensions
{
    public static TransportOperation ToTransportOperation(this IRoutingContext context, RoutingStrategy strategy, DispatchConsistency dispatchConsistency, bool copySharedMutableMessageState)
    {
        var headers = copySharedMutableMessageState ? new Dictionary<string, string>(context.Message.Headers) : context.Message.Headers;
        var dispatchProperties = context.Extensions.TryGet<DispatchProperties>(out var properties)
            ? copySharedMutableMessageState ? new DispatchProperties(properties) : properties
            : [];
        var addressLabel = strategy.Apply(headers);
        var message = new OutgoingMessage(context.Message.MessageId, headers, context.Message.Body);

        var transportOperation = new TransportOperation(message, addressLabel, dispatchProperties, dispatchConsistency);
        return transportOperation;
    }
}