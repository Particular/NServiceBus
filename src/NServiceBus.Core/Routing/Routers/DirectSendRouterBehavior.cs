namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Unicast.Queuing;

    class DirectSendRouterBehavior : Behavior<OutgoingSendContext>
    {
        IDirectRoutingStrategy directRoutingStrategy;
        DistributionPolicy distributionPolicy;

        public DirectSendRouterBehavior(string localAddress, 
            IDirectRoutingStrategy directRoutingStrategy, 
            DistributionPolicy distributionPolicy)
        {
            this.localAddress = localAddress;
            this.directRoutingStrategy = directRoutingStrategy;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(OutgoingSendContext context, Func<Task> next)
        {
            var messageType = context.Message.MessageType;
            var distributionStrategy = distributionPolicy.GetDistributionStrategy(messageType);

            var state = context.GetOrCreate<State>();
            var destination = state.ExplicitDestination ?? (state.RouteToLocalInstance ? localAddress : null);

            var addressLabels = string.IsNullOrEmpty(destination) 
                ? directRoutingStrategy.Route(messageType, distributionStrategy, context) 
                : RouteToDestination(destination);

            context.SetAddressLabels(addressLabels.EnsureNonEmpty(() => "No destination specified for message: " + messageType));
            context.SetHeader(Headers.MessageIntent, MessageIntentEnum.Send.ToString());
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. You may have misconfigured the destination for this kind of message ({messageType}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex);
            }
        }

        static IEnumerable<AddressLabel> RouteToDestination(string physicalAddress)
        {
            yield return new DirectAddressLabel(physicalAddress);
        }

        DynamicRoutingProvider dynamicRouting;
        string localAddress;
        MessageRouter messageRouter;

        public class State
        {
            public string ExplicitDestination { get; set; }
            public bool RouteToLocalInstance { get; set; }
        }
    }
}