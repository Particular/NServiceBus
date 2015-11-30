namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class UnicastRouterTests
    {
        UnicastRouter router;
        MessageMetadataRegistry metadataRegistry;
        UnicastRoutingTable routingTable;
        EndpointInstances endpointInstances;
        TransportAddresses transportAddresses;

        [Test]
        public void Should_route_a_command_to_a_single_non_scaled_out_destination()
        {
            var sales = new EndpointName("Sales");
            metadataRegistry.RegisterMessageType(typeof(Command));
            routingTable.AddStatic(typeof(Command), sales);
            endpointInstances.AddStatic(sales, new EndpointInstanceName(sales, null, null));
            transportAddresses.AddRule(i => i.ToString());

            var routes = router.Route(typeof(Command), new SingleInstanceRoundRobinDistributionStrategy(), new ContextBag()).Result.ToArray();
            
            Assert.AreEqual(1, routes.Length);
            var headers = new Dictionary<string, string>();
            var addressTag = (UnicastAddressTag) routes[0].Apply(headers);
            Assert.AreEqual("Sales", addressTag.Destination);
        }

        [Test]
        public void Should_route_an_event_to_a_single_non_scaled_out_destination()
        {
            var sales = new EndpointName("Sales");
            metadataRegistry.RegisterMessageType(typeof(Event));
            routingTable.AddStatic(typeof(Event), sales);
            endpointInstances.AddStatic(sales, new EndpointInstanceName(sales, null, null));
            transportAddresses.AddRule(i => i.ToString());

            var routes = router.Route(typeof(Event), new SingleInstanceRoundRobinDistributionStrategy(), new ContextBag()).Result.ToArray();

            Assert.AreEqual(1, routes.Length);
            Assert.AreEqual("Sales", ExtractDestination(routes[0]));
        }

        [Test]
        public void Should_route_an_event_to_a_single_instance_of_each_endpoint()
        {
            var sales = new EndpointName("Sales");
            var shipping = new EndpointName("Shipping");
            metadataRegistry.RegisterMessageType(typeof(Event));
            routingTable.AddStatic(typeof(Event), sales);
            routingTable.AddStatic(typeof(Event), shipping);

            endpointInstances.AddStatic(sales, new EndpointInstanceName(sales, "1", null));
            endpointInstances.AddDynamic(e => new[] { new EndpointInstanceName(sales, "2", null)});
            endpointInstances.AddStatic(shipping, new EndpointInstanceName(shipping, "1", null), new EndpointInstanceName(shipping, "2", null));

            transportAddresses.AddRule(i => i.ToString());

            var routes = router.Route(typeof(Event), new SingleInstanceRoundRobinDistributionStrategy(), new ContextBag()).Result.ToArray();

            Assert.AreEqual(2, routes.Length);
            Assert.AreEqual("Sales-1", ExtractDestination(routes[0]));
            Assert.AreEqual("Shipping-1", ExtractDestination(routes[1]));
        }

        static string ExtractDestination(UnicastRoutingStrategy route)
        {
            var headers = new Dictionary<string, string>();
            var addressTag = (UnicastAddressTag) route.Apply(headers);
            var destination = addressTag.Destination;
            return destination;
        }

        [SetUp]
        public void Setup()
        {
            metadataRegistry = new MessageMetadataRegistry(new Conventions());
            routingTable = new UnicastRoutingTable();
            endpointInstances = new EndpointInstances();
            transportAddresses = new TransportAddresses();
            router = new UnicastRouter(
                metadataRegistry,
                routingTable,
                endpointInstances,
                transportAddresses);
        }

        class Command : ICommand
        {
        }

        class Event : IEvent
        {
        }
    }
}