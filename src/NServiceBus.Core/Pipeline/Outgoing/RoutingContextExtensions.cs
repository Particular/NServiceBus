namespace NServiceBus
{
    using Pipeline;
    using Routing;
    using Transport;

    static class RoutingContextExtensions
    {
        public static TransportOperation ToTransportOperation(this IRoutingContext context, RoutingStrategy strategy, DispatchConsistency dispatchConsistency)
        {
            var addressLabel = strategy.Apply(context.Message.Headers);
            var message = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);

            if (!context.Extensions.TryGet(out DispatchProperties dispatchProperties))
            {
                dispatchProperties = new DispatchProperties();
            }

            var transportOperation = new TransportOperation(message, addressLabel, dispatchProperties, dispatchConsistency);
            return transportOperation;
        }
    }
}