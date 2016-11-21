namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    class UnicastDistributionConnector : StageConnector<IOutgoingDistributionContext, IOutgoingLogicalMessageContext>
    {
        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;
        IDistributionPolicy defaultDistributionPolicy;

        public UnicastDistributionConnector(EndpointInstances endpointInstances, DistributionPolicy distributionPolicy, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
            this.defaultDistributionPolicy = distributionPolicy;
        }

        public override Task Invoke(IOutgoingDistributionContext context, Func<IOutgoingLogicalMessageContext, Task> stage)
        {
            var state = context.Extensions.GetOrCreate<UnicastSendRouterConnector.State>();

            var distributionPolicy = state.Option == UnicastRouteOption.RouteToSpecificInstance
                ? new SpecificInstanceDistributionPolicy(state.SpecificInstance, transportAddressTranslation)
                : defaultDistributionPolicy;

            var destinations = SelectDestinationsForEachEndpoint(distributionPolicy, context.Destinations, context.DistributionScope);

            var downstreamContext = this.CreateOutgoingLogicalMessageContext(context.Message, destinations, context);

            return stage(downstreamContext);
        }


        IReadOnlyCollection<UnicastRoutingStrategy> SelectDestinationsForEachEndpoint(IDistributionPolicy distributionPolicy, IEnumerable<UnicastRoute> routes, DistributionStrategyScope distributionScope)
        {
            //Make sure we are sending only one to each transport destination. Might happen when there are multiple routing information sources.
            var addresses = new HashSet<string>();
            var destinationsByEndpoint = routes
                .GroupBy(d => d.Endpoint, d => d);

            foreach (var group in destinationsByEndpoint)
            {
                if (group.Key == null) //Routing targets that do not specify endpoint name
                {
                    //Send a message to each target as we have no idea which endpoint they represent
                    foreach (var destination in group.SelectMany(ResolveTransportAddress))
                    {
                        addresses.Add(destination);
                    }
                }
                else
                {

                    var candidateInstances = group.SelectMany(ResolveTransportAddress).ToArray();
                    var selectReceiver = distributionPolicy.GetDistributionStrategy(group.Key, distributionScope).SelectReceiver(candidateInstances);
                    addresses.Add(selectReceiver);
                }
            }

            return addresses.Select(a => new UnicastRoutingStrategy(a)).ToArray();
        }

        IEnumerable<string> ResolveTransportAddress(UnicastRoute destination)
        {
            if (destination.Instance != null)
            {
                yield return transportAddressTranslation(destination.Instance);
            }
            else if (destination.PhysicalAddress != null)
            {
                yield return destination.PhysicalAddress;
            }
            else
            {
                var instances = endpointInstances.FindInstances(destination.Endpoint);
                foreach (var address in instances.Select(i => transportAddressTranslation(i)))
                {
                    yield return address;
                }
            }
        }
    }
}