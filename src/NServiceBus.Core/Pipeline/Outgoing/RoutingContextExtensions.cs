namespace NServiceBus
{
    using System.Collections.Generic;
    using Pipeline;
    using Routing;
    using Transport;

    static class RoutingContextExtensions
    {
        public static TransportOperation ToTransportOperation(this IRoutingContext context, RoutingStrategy strategy, DispatchConsistency dispatchConsistency, bool copySharedMutableMessageState)
        {
            var headers = copySharedMutableMessageState ? new Dictionary<string, string>(context.Message.Headers) : context.Message.Headers;
            var dispatchProperties = context.Extensions.TryGet(out DispatchProperties properties)
                ? copySharedMutableMessageState ? new DispatchProperties(properties) : properties
                : new DispatchProperties();
            var addressLabel = strategy.Apply(headers);
            var message = new OutgoingMessage(context.Message.MessageId, headers, context.Message.Body);

            var transportOperation = new TransportOperation(message, addressLabel, dispatchProperties, dispatchConsistency);
            return transportOperation;
        }
    }
}