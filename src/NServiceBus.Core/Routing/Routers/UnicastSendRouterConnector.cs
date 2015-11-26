namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Queuing;

    class UnicastSendRouterConnector : StageConnector<OutgoingSendContext, OutgoingLogicalMessageContext>
    {
        IUnicastRouter unicastRouter;
        DistributionPolicy distributionPolicy;

        public UnicastSendRouterConnector(string localAddress, 
            IUnicastRouter unicastRouter, 
            DistributionPolicy distributionPolicy)
        {
            this.localAddress = localAddress;
            this.unicastRouter = unicastRouter;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(OutgoingSendContext context, Func<OutgoingLogicalMessageContext, Task> next)
        {
            var messageType = context.Message.MessageType;
            var distributionStrategy = distributionPolicy.GetDistributionStrategy(messageType);

            var state = context.GetOrCreate<State>();
            var destination = state.ExplicitDestination ?? (state.RouteToLocalInstance ? localAddress : null);

            var addressLabels = string.IsNullOrEmpty(destination) 
                ? await unicastRouter.Route(messageType, distributionStrategy, context) 
                : RouteToDestination(destination);

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

            var logicalMessageContext = new OutgoingLogicalMessageContext(
                    context.MessageId,
                    context.Headers,
                    context.Message,
                    addressLabels.EnsureNonEmpty(() => "No destination specified for message: " + messageType).ToArray(),
                    context);

            try
            {
                await next(logicalMessageContext).ConfigureAwait(false);
            }
            catch (QueueNotFoundException ex)
            {
                throw new Exception($"The destination queue '{ex.Queue}' could not be found. You may have misconfigured the destination for this kind of message ({messageType}) in the MessageEndpointMappings of the UnicastBusConfig section in your configuration file. " + "It may also be the case that the given queue just hasn't been created yet, or has been deleted.", ex);
            }
        }

        static IEnumerable<UnicastRoutingStrategy> RouteToDestination(string physicalAddress)
        {
            yield return new UnicastRoutingStrategy(physicalAddress);
        }

        string localAddress;

        public class State
        {
            public string ExplicitDestination { get; set; }
            public bool RouteToLocalInstance { get; set; }
        }
    }
}