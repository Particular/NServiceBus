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

    class UnicastSendRouterConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        string localAddress;
        readonly EndpointInstance localEndpointInstance;
        readonly LogicalToTransportAddressTranslation addressTranslator;
        IUnicastRouter unicastRouter;
        DistributionPolicy distributionPolicy;

        public UnicastSendRouterConnector(string localAddress, EndpointInstance localEndpointInstance, LogicalToTransportAddressTranslation addressTranslator, IUnicastRouter unicastRouter, DistributionPolicy distributionPolicy)
        {
            this.localAddress = localAddress;
            this.localEndpointInstance = localEndpointInstance;
            this.addressTranslator = addressTranslator;
            this.unicastRouter = unicastRouter;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            var messageType = context.Message.MessageType;
            var distributionStrategy = distributionPolicy.GetDistributionStrategy(messageType);

            var state = context.Extensions.GetOrCreate<State>();

            var addressLabels = state.Routing == State.RoutingType.ByMessageType 
                ? await unicastRouter.Route(messageType, distributionStrategy, context.Extensions).ConfigureAwait(false) 
                : RouteToDestination(state);

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

        IEnumerable<UnicastRoutingStrategy> RouteToDestination(State state)
        {
            if (state.Routing == State.RoutingType.ExplicitDestination)
            {
                yield return new UnicastRoutingStrategy(state.ExplicitDestination);
            }
            else if (state.Routing == State.RoutingType.LocalInstance)
            {
                yield return new UnicastRoutingStrategy(localAddress);
            }
            else
            {
                var satelliteLogicalAddress = new LogicalAddress(localEndpointInstance, state.Satellite);
                yield return new UnicastRoutingStrategy(addressTranslator.Translate(satelliteLogicalAddress));
            }
        }

        public class State
        {
            public State() 
            {
                Routing = RoutingType.ByMessageType;
            }

            public string ExplicitDestination { get; private set; }
            public RoutingType Routing { get; private set; }
            public string Satellite { get; private set; }

            public void RouteExplicit(string explicitDestination)
            {
                Routing = RoutingType.ExplicitDestination;
                ExplicitDestination = explicitDestination;
            }

            public void RouteLocalInstance()
            {
                Routing = RoutingType.LocalInstance;
            }

            public void RouteToSatellite(string satellite)
            {
                Routing = RoutingType.Satellite;
                Satellite = satellite;
            }

            public enum RoutingType
            {
               ByMessageType,
               ExplicitDestination,
               LocalInstance,
               Satellite
            }
        }
    }
}