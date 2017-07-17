namespace NServiceBus
{
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transport;

    static class MessageOperationsInvokeHandlerContext
    {
        public static Task HandleCurrentMessageLater(IInvokeHandlerContext context)
        {
            if (context.HandleCurrentMessageLaterWasCalled)
            {
                return TaskEx.CompletedTask;
            }

            var messageBeingProcessed = context.Extensions.Get<IncomingMessage>();

            var cache = context.Extensions.Get<IPipelineCache>();
            var pipeline = cache.Pipeline<IRoutingContext>();

            var outgoingMessage = new OutgoingMessage(
                messageBeingProcessed.MessageId,
                messageBeingProcessed.Headers,
                messageBeingProcessed.Body);

            var routingContext = new RoutingContext(outgoingMessage, RouteToThisEndpointStrategy.Instance, context);

            return pipeline.Invoke(routingContext);
        }
    }
}