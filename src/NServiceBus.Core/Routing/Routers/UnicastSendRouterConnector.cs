namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Queuing;

    class UnicastSendRouterConnector : StageConnector<IOutgoingSendContext, IOutgoingLogicalMessageContext>
    {
        IUnicastRouter unicastRouter;
        DistributionPolicy distributionPolicy;
        string sharedQueue;
        string instanceSpecificQueue;

        public UnicastSendRouterConnector(
            string sharedQueue, 
            string instanceSpecificQueue,
            IUnicastRouter unicastRouter, 
            DistributionPolicy distributionPolicy)
        {
            this.sharedQueue = sharedQueue;
            this.instanceSpecificQueue = instanceSpecificQueue;
            this.unicastRouter = unicastRouter;
            this.distributionPolicy = distributionPolicy;
        }

        public override async Task Invoke(IOutgoingSendContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var messageType = context.Message.MessageType;

            Destination customDestination;
            context.Extensions.TryGet(out customDestination);
            
            if (customDestination == Destination.ThisInstance && instanceSpecificQueue == null)
            {
                throw new InvalidOperationException("Cannot route to this specific instance because endpoint instance ID was not provided by either host, a plugin or user. You can specify it via BusConfiguration.EndpointInstanceId, use a specific host or plugin.");
            }

            var thisEndpoint = customDestination == Destination.ThisEndpoint ? sharedQueue : null;
            var thisInstance = customDestination == Destination.ThisInstance ? instanceSpecificQueue : null;
            var explicitDestination = customDestination?.Option == Destination.RouteOption.ExplicitDestination ? customDestination.Value : null;
            var destination = explicitDestination ?? thisInstance ?? thisEndpoint;

            DistributionStrategy distributionStrategy;

            if (customDestination?.Option == Destination.RouteOption.RouteToSpecificInstance)
            {
                distributionStrategy = new SpecificInstanceDistributionStrategy(customDestination.Value);
            }
            else
            {
                distributionStrategy = distributionPolicy.GetDistributionStrategy(messageType);
            }

            var routingStrategies = string.IsNullOrEmpty(destination) 
                ? await unicastRouter.Route(messageType, distributionStrategy, context.Extensions).ConfigureAwait(false) 
                : RouteToDestination(destination);

            context.Headers[Headers.MessageIntent] = MessageIntentEnum.Send.ToString();

            var logicalMessageContext = this.CreateOutgoingLogicalMessageContext(
                context.Message, 
                routingStrategies.EnsureNonEmpty(() => "No destination specified for message: " + messageType).ToArray(), 
                context);

            try
            {
                await stage(logicalMessageContext).ConfigureAwait(false);
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
        class SpecificInstanceDistributionStrategy : DistributionStrategy
        {
            string specificInstance;

            public SpecificInstanceDistributionStrategy(string specificInstance)
            {
                this.specificInstance = specificInstance;
            }

            public override IEnumerable<UnicastRoutingTarget> SelectDestination(IEnumerable<UnicastRoutingTarget> allInstances)
            {
                var target = allInstances.FirstOrDefault(t => t.Instance != null && t.Instance.Discriminator == specificInstance);
                if (target == null)
                {
                    throw new Exception($"Specified instance {specificInstance} has not been configured in the routing tables.");
                }
                yield return target;
            }
        }
    }
}