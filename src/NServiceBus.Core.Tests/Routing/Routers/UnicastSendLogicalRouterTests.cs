namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class UnicastSendLogicalRouterTests
    {

        [Test]
        public void When_routing_command_to_logical_endpoint_without_configured_instances_should_route_to_a_single_destination()
        {
            var logicalEndpointName = "Sales";
            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointName(logicalEndpointName))
            });

            var context = CreateContext(new SendOptions(), new Command());

            var router = CreateRouter(routingTable);
            var route = router.Route(context);

            Assert.AreEqual(logicalEndpointName, ExtractDestination(route));
        }

        [Test]
        public void When_multiple_dynamic_instances_for_logical_endpoints_should_route_message_to_a_single_instance()
        {
            var sales = "Sales";
            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointName(sales))
            });

            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(sales, "1"),
                new EndpointInstance(sales, "2"),
            });

            var context = CreateContext(new SendOptions(), new Command());

            var router = CreateRouter(routingTable, endpointInstances);
            var route = router.Route(context);

            Assert.AreEqual("Sales-1", ExtractDestination(route));
        }

        [Test]
        public void When_multiple_dynamic_instances_for_logical_endpoints_should_round_robin()
        {
            var sales = "Sales";
            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointName(sales))
            });

            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(sales, "1"),
                new EndpointInstance(sales, "2"),
            });

            var context = CreateContext(new SendOptions(), new Command());

            var router = CreateRouter(routingTable, endpointInstances);
            var route1 = router.Route(context);
            var route2 = router.Route(context);
            var route3 = router.Route(context);

            Assert.AreEqual("Sales-1", ExtractDestination(route1));
            Assert.AreEqual("Sales-2", ExtractDestination(route2));
            Assert.AreEqual("Sales-1", ExtractDestination(route3));
        }

        [Test]
        public void Should_return_null_when_no_routes_found()
        {
            var context = CreateContext(new SendOptions(), new Command());

            var router = CreateRouter();

            var route = router.Route(context);

            Assert.IsNull(route);
        }

        [Test]
        public void Should_route_to_local_endpoint_if_requested_so()
        {
            var options = new SendOptions();

            options.RouteToThisEndpoint();

            var context = CreateContext(options, new Command());

            var router = CreateRouter();
            var route = router.Route(context);

            Assert.AreEqual("Endpoint", ExtractDestination(route));
        }

        [Test]
        public void When_multiple_dynamic_instances_for_local_endpoint_should_route_message_to_a_single_instance()
        {
            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance("Endpoint", "1"),
                new EndpointInstance("Endpoint", "2"),
            });

            var options = new SendOptions();

            options.RouteToThisEndpoint();

            var context = CreateContext(options, new Command());

            var router = CreateRouter(instances: endpointInstances);
            var route = router.Route(context);

            Assert.AreEqual("Endpoint-1", ExtractDestination(route));
        }

        [Test]
        public void When_multiple_dynamic_instances_for_local_endpoint_and_instance_selected_should_route_to_instance()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointName("Endpoint"))
            });

            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance("Endpoint", "1"),
                new EndpointInstance("Endpoint", "2"),
            });

            var options = new SendOptions();

            options.RouteToSpecificInstance("2");

            var context = CreateContext(options, new Command());

            var router = CreateRouter(routingTable, endpointInstances);
            var route = router.Route(context);

            Assert.AreEqual("Endpoint-2", ExtractDestination(route));
        }

        [Test]
        public void When_multiple_dynamic_instances_for_local_endpoint_and_instance_selected_should_not_round_robin()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointName("Endpoint"))
            });

            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance("Endpoint", "1"),
                new EndpointInstance("Endpoint", "2"),
            });

            var options = new SendOptions();

            options.RouteToSpecificInstance("2");

            var context = CreateContext(options, new Command());

            var router = CreateRouter(routingTable, endpointInstances);
            var route1 = router.Route(context);
            var route2 = router.Route(context);
            var route3 = router.Route(context);

            Assert.AreEqual("Endpoint-2", ExtractDestination(route1));
            Assert.AreEqual("Endpoint-2", ExtractDestination(route2));
            Assert.AreEqual("Endpoint-2", ExtractDestination(route3));
        }

        [Test]
        public void When_multiple_dynamic_instances_for_local_endpoint_should_round_robin()
        {
            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance("Endpoint", "1"),
                new EndpointInstance("Endpoint", "2"),
            });

            var options = new SendOptions();

            options.RouteToThisEndpoint();

            var context = CreateContext(options, new Command());

            var router = CreateRouter(instances: endpointInstances);

            var route1 = router.Route(context);
            var route2 = router.Route(context);
            var route3 = router.Route(context);

            Assert.AreEqual("Endpoint-1", ExtractDestination(route1));
            Assert.AreEqual("Endpoint-2", ExtractDestination(route2));
            Assert.AreEqual("Endpoint-1", ExtractDestination(route3));
        }

        [Test]
        public void When_route_with_physical_address_routes_to_physical_address()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromPhysicalAddress("Physical"))
            });

            var context = CreateContext(new SendOptions(), new Command());

            var router = CreateRouter(routingTable);
            var route = router.Route(context);

            Assert.AreEqual("Physical", ExtractDestination(route));
        }

        [Test]
        public void When_route_with_endpoint_instance_routes_to_instance()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointInstance(new EndpointInstance("Endpoint", "2")))
            });

            var context = CreateContext(new SendOptions(), new Command());

            var router = CreateRouter(routingTable);
            var route = router.Route(context);

            Assert.AreEqual("Endpoint-2", ExtractDestination(route));
        }

        static string ExtractDestination(UnicastRoutingStrategy route)
        {
            var headers = new Dictionary<string, string>();
            var addressTag = (UnicastAddressTag) route.Apply(headers);
            return addressTag.Destination;
        }

        static UnicastSend.LogicalRouter CreateRouter(UnicastRoutingTable routingTable = null, EndpointInstances instances = null, DistributionPolicy policy = null)
        {
            var table = routingTable ?? new UnicastRoutingTable();
            var inst = instances ?? new EndpointInstances();
            var pol = policy ?? new DistributionPolicy();

            return new UnicastSend.LogicalRouter("sharedQueue", null, "Endpoint", pol, table, inst, i => i.ToString());
        }

        class Command : ICommand
        {
        }

        static IOutgoingSendContext CreateContext(SendOptions options, object message = null)
        {
            var context = new TestableOutgoingSendContext
            {
                Message = new OutgoingLogicalMessage(message.GetType(), message),
                Extensions = options.Context
            };
            return context;
        }
    }
}