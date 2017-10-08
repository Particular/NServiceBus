namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class UnicastSendRouterTests
    {
        [Test]
        public void Should_use_explicit_route_for_sends_if_present()
        {
            var router = CreateRouter(null);
            var options = new SendOptions();

            options.SetDestination("destination endpoint");

            var context = CreateContext(options);

            var result = router.Route(context);
            Assert.AreEqual("destination endpoint", ExtractDestination(result));
        }


        [Test]
        public void Should_route_to_local_instance_if_requested_so()
        {
            var router = CreateRouter("MyInstance");
            var options = new SendOptions();

            options.RouteToThisInstance();

            var context = CreateContext(options);

            var result = router.Route(context);

            Assert.AreEqual("MyInstance", ExtractDestination(result));
        }

        [Test]
        public void Should_throw_if_requested_to_route_to_local_instance_and_instance_has_no_specific_queue()
        {
            var router = CreateRouter(null);

            var options = new SendOptions();

            options.RouteToThisInstance();

            var context = CreateContext(options);

            var exception = Assert.Throws<InvalidOperationException>(() => router.Route(context));
            Assert.AreEqual(exception.Message, "Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
        }

        [Test]
        public void When_routing_to_specific_instance_should_throw_when_there_is_no_route_for_given_type()
        {
            var router = CreateRouter(null);
            var options = new SendOptions();

            options.RouteToSpecificInstance("instanceId");

            var context = CreateContext(options);

            var exception = Assert.Throws<Exception>(() => router.Route(context));
            StringAssert.Contains("No destination specified for message", exception.Message);
        }

        [Test]
        public void When_routing_to_specific_instance_should_throw_when_route_for_given_type_points_to_physical_address()
        {
            var table = new UnicastRoutingTable();
            table.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromPhysicalAddress("PhysicalAddress"))
            });
            var router = CreateRouter(table);
            var options = new SendOptions();

            options.RouteToSpecificInstance("instanceId");

            var context = CreateContext(options);

            var exception = Assert.Throws<Exception>(() => router.Route(context));
            StringAssert.Contains("Routing to a specific instance is only allowed if route is defined for a logical endpoint, not for an address or instance.", exception.Message);
        }

        [Test]
        public void When_routing_to_specific_instance_should_select_appropriate_instance()
        {
            var table = new UnicastRoutingTable();
            var instances = new EndpointInstances();
            table.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromEndpointName("Endpoint"))
            });
            instances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance("Endpoint", "1"),
                new EndpointInstance("Endpoint", "2"),
                new EndpointInstance("Endpoint", "3")
            });
            var router = CreateRouter(table, instances);
            var options = new SendOptions();

            options.RouteToSpecificInstance("2");

            var context = CreateContext(options);

            var route = router.Route(context);
            Assert.AreEqual("Endpoint-2", ExtractDestination(route));
        }


        [Test]
        public void When_routing_command_to_logical_endpoint_without_configured_instances_should_route_to_a_single_destination()
        {
            var logicalEndpointName = "Sales";
            var routingTable = new UnicastRoutingTable();
            routingTable.AddOrReplaceRoutes("A", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromEndpointName(logicalEndpointName))
            });

            var context = CreateContext(new SendOptions(), new MyMessage());

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
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromEndpointName(sales))
            });

            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(sales, "1"),
                new EndpointInstance(sales, "2"),
            });

            var context = CreateContext(new SendOptions(), new MyMessage());

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
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromEndpointName(sales))
            });

            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(sales, "1"),
                new EndpointInstance(sales, "2"),
            });

            var context = CreateContext(new SendOptions(), new MyMessage());

            var router = CreateRouter(routingTable, endpointInstances);
            var route1 = router.Route(context);
            var route2 = router.Route(context);
            var route3 = router.Route(context);

            Assert.AreEqual("Sales-1", ExtractDestination(route1));
            Assert.AreEqual("Sales-2", ExtractDestination(route2));
            Assert.AreEqual("Sales-1", ExtractDestination(route3));
        }

        [Test]
        public void Should_throw_when_no_routes_found()
        {
            var context = CreateContext(new SendOptions(), new MyMessage());

            var router = CreateRouter();

            var exception = Assert.Throws<Exception>(() => router.Route(context));
            StringAssert.Contains("No destination specified for message", exception.Message);
        }

        [Test]
        public void Should_route_to_local_endpoint_if_requested_so()
        {
            var options = new SendOptions();

            options.RouteToThisEndpoint();

            var context = CreateContext(options, new MyMessage());

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

            var context = CreateContext(options, new MyMessage());

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
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromEndpointName("Endpoint"))
            });

            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance("Endpoint", "1"),
                new EndpointInstance("Endpoint", "2"),
            });

            var options = new SendOptions();

            options.RouteToSpecificInstance("2");

            var context = CreateContext(options, new MyMessage());

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
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromEndpointName("Endpoint"))
            });

            var endpointInstances = new EndpointInstances();
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance("Endpoint", "1"),
                new EndpointInstance("Endpoint", "2"),
            });

            var options = new SendOptions();

            options.RouteToSpecificInstance("2");

            var context = CreateContext(options, new MyMessage());

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

            var context = CreateContext(options, new MyMessage());

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
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromPhysicalAddress("Physical"))
            });

            var context = CreateContext(new SendOptions(), new MyMessage());

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
                new RouteTableEntry(typeof(MyMessage), UnicastRoute.CreateFromEndpointInstance(new EndpointInstance("Endpoint", "2")))
            });

            var context = CreateContext(new SendOptions(), new MyMessage());

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

        static UnicastSendRouter CreateRouter(string instanceSpecificQueue)
        {
            return new UnicastSendRouter("Endpoint", instanceSpecificQueue, new DistributionPolicy(), new UnicastRoutingTable(), new EndpointInstances(), i => i.ToString());
        }

        static UnicastSendRouter CreateRouter(UnicastRoutingTable routingTable = null, EndpointInstances instances = null, DistributionPolicy policy = null)
        {
            var table = routingTable ?? new UnicastRoutingTable();
            var inst = instances ?? new EndpointInstances();
            var pol = policy ?? new DistributionPolicy();

            return new UnicastSendRouter("Endpoint", null, pol, table, inst, i => i.ToString());
        }

        class MyMessage : ICommand
        {
        }

        static IOutgoingSendContext CreateContext(SendOptions options, object message = null)
        {
            if (message == null)
            {
                message = new MyMessage();
            }

            var context = new TestableOutgoingSendContext
            {
                Message = new OutgoingLogicalMessage(message.GetType(), message),
                Extensions = options.Context
            };
            return context;
        }
    }
}