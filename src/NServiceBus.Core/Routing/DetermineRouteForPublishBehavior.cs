namespace NServiceBus
{
    using System;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;

    class DetermineRouteForPublishBehavior : Behavior<OutgoingPublishContext>
    {
        public override void Invoke(OutgoingPublishContext context, Action next)
        {
            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Publish.ToString());

            context.Set<RoutingStrategy>(new ToAllSubscribers(context.GetMessageType()));

            next();
        }
    }
}