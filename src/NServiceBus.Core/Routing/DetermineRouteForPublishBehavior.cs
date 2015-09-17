namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;

    class DetermineRouteForPublishBehavior : Behavior<OutgoingPublishContext>
    {
        public override Task Invoke(OutgoingPublishContext context, Func<Task> next)
        {
            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Publish.ToString());

            context.Set<RoutingStrategy>(new ToAllSubscribers(context.GetMessageType()));

            return next();
        }
    }
}