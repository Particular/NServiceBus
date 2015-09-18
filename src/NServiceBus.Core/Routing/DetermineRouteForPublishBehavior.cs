namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;
    using Routing;
    using TransportDispatch;

    class DetermineRouteForPublishBehavior : Behavior<OutgoingPublishContext>
    {
        public override Task Invoke(OutgoingPublishContext context, Func<Task> next)
        {
            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Publish.ToString());

            context.Set<RoutingStrategy>(new ToAllSubscribers(context.Message.MessageType));

            return next();
        }
    }
}