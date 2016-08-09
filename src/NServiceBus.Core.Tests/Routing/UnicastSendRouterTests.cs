namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class UnicastSendRouterTests
    {
        UnicastSendRouter router;
        UnicastRoutingTable routingTable;
        EndpointInstances endpointInstances;

        [Test]
        public void When_routing_command_to_logical_endpoint_without_configured_instances_should_route_to_a_single_destination()
        {
            var logicalEndpointName = "Sales";
            routingTable.AddOrReplaceRoutes(Guid.NewGuid(), new List<RouteTableEntry> {new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointName(logicalEndpointName)) });

            var routes = router.Route(typeof(Command), new DistributionPolicy());

            Assert.AreEqual(1, routes.Count());
            Assert.AreEqual(logicalEndpointName, ExtractDestination(routes.First()));
        }

        [Test]
        public void When_multiple_dynamic_instances_for_logical_endpoints_should_route_message_to_a_single_instance()
        {
            var sales = "Sales";
            routingTable.AddOrReplaceRoutes(Guid.NewGuid(), new List<RouteTableEntry> { new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointName(sales)) });

            endpointInstances.AddOrReplaceInstances(Guid.NewGuid(), new List<EndpointInstance>
            {
                new EndpointInstance(sales, "1"),
                new EndpointInstance(sales, "2"),
            });

            var routes = router.Route(typeof(Command), new DistributionPolicy()).ToArray();

            Assert.AreEqual(1, routes.Length);
            Assert.AreEqual("Sales-1", ExtractDestination(routes[0]));
        }

        [Test]
        public void Should_return_empty_list_when_no_routes_found()
        {
            var routes = router.Route(typeof(Command), new DistributionPolicy());

            Assert.IsEmpty(routes);
        }

        static string ExtractDestination(UnicastRoutingStrategy route)
        {
            var headers = new Dictionary<string, string>();
            var addressTag = (UnicastAddressTag) route.Apply(headers);
            return addressTag.Destination;
        }

        [SetUp]
        public void Setup()
        {
            routingTable = new UnicastRoutingTable();
            endpointInstances = new EndpointInstances();
            router = new UnicastSendRouter(
                routingTable,
                endpointInstances,
                i => i.ToString());
        }

        class Command : ICommand
        {
        }
    }
}