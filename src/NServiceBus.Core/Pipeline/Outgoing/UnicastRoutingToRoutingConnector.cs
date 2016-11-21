namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;

    class UnicastRoutingToRoutingConnector : StageConnector<IUnicastRoutingContext, IRoutingContext>
    {
        EndpointInstances endpointInstances;
        Func<EndpointInstance, string> transportAddressTranslation;

        public UnicastRoutingToRoutingConnector(EndpointInstances endpointInstances, Func<EndpointInstance, string> transportAddressTranslation)
        {
            this.endpointInstances = endpointInstances;
            this.transportAddressTranslation = transportAddressTranslation;
        }

        public override Task Invoke(IUnicastRoutingContext context, Func<IRoutingContext, Task> stage)
        {
            var destinations = SelectDestinationsForEachEndpoint(context.Destinations, context.DistributionFunction);

            var downstreamContext = this.CreateRoutingContext(context.Message, destinations, context);

            return stage(downstreamContext);
        }

        IReadOnlyCollection<UnicastRoutingStrategy> SelectDestinationsForEachEndpoint(IEnumerable<UnicastRoute> routes, Func<string[], string[]> distributionFunction)
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
                    var selectReceivers = distributionFunction(candidateInstances);
                    foreach (var receiver in selectReceivers)
                    {
                        addresses.Add(receiver);
                    }
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