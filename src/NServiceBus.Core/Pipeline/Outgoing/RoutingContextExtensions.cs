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
        Dictionary<string, string> headers;
        if (copySharedMutableMessageState)
        {
            headers = DictionaryPool<string, string>.Shared.Rent(context.Message.Headers.Count);
            foreach (var kvp in context.Message.Headers)
            {
                headers[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            headers = context.Message.Headers;
        }
        var dispatchProperties = context.Extensions.TryGet<DispatchProperties>(out var properties)
            ? copySharedMutableMessageState ? new DispatchProperties(properties) : properties
            : [];
        var addressLabel = strategy.Apply(headers);
        var message = new OutgoingMessage(context.Message.MessageId, headers, context.Message.Body);

        var transportOperation = new TransportOperation(message, addressLabel, dispatchProperties, dispatchConsistency);
        return transportOperation;
    }
}