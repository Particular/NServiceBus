namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.StorageDrivenPublishing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Unicast.Queuing;

    class DirectPublishRouterBehavior : Behavior<OutgoingPublishContext>
    {
        DirectRoutingStrategy directRoutingStrategy;
        DistributionPolicy distributionPolicy;

        public DirectPublishRouterBehavior(DirectRoutingStrategy directRoutingStrategy, DistributionPolicy distributionPolicy)
        {
            this.directRoutingStrategy = directRoutingStrategy;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(OutgoingPublishContext context, Func<Task> next)
        {
            var eventType = context.GetMessageType();
            var distributionStrategy = distributionPolicy.GetDistributionStrategy(eventType);

            var addressLabels = directRoutingStrategy.Route(eventType, distributionStrategy, context).ToList();

            context.SetAddressLabels(addressLabels.EnsureNonEmpty(() => "No destination specified for message: " + eventType));
            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Send.ToString());
            context.Set(new SubscribersForEvent(addressLabels.OfType<DirectAddressLabel>().Select(r => r.Destination).ToList(), eventType));
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. You may have misconfigured the destination for this kind of message ({eventType}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex);
            }
        }
    }
}