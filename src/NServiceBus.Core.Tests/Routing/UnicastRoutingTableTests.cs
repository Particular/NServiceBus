namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    class UnicastRoutingTableTests
    {
        [Test]
        public void When_group_does_not_exist_should_add_routes()
        {
            var routingTable = new UnicastRoutingTable();
            var route = UnicastRoute.CreateFromEndpointName("Endpoint1");
            routingTable.AddOrReplaceRoutes("key", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), route),
            });

            var retrievedRoute = routingTable.GetRouteFor(typeof(Command));
            Assert.AreSame(route, retrievedRoute);
        }

        [Test]
        public void When_group_exists_should_replace_existing_routes()
        {
            var routingTable = new UnicastRoutingTable();
            var oldRoute = UnicastRoute.CreateFromEndpointName("Endpoint1");
            var newRoute = UnicastRoute.CreateFromEndpointName("Endpoint2");
            routingTable.AddOrReplaceRoutes("key", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), oldRoute),
            });

            routingTable.AddOrReplaceRoutes("key", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), newRoute),
            });

            var retrievedRoute = routingTable.GetRouteFor(typeof(Command));
            Assert.AreSame(newRoute, retrievedRoute);
        }

        [Test]
        public void When_routes_are_ambiguous_should_throw_exception()
        {
            var routingTable = new UnicastRoutingTable();
            var lowPriorityRoute = UnicastRoute.CreateFromEndpointName("Endpoint1");
            var highPriorityRoute = UnicastRoute.CreateFromEndpointName("Endpoint2");

            routingTable.AddOrReplaceRoutes("key2", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), highPriorityRoute),
            });

            Assert.That(() =>
            {
                routingTable.AddOrReplaceRoutes("key1", new List<RouteTableEntry>
                {
                    new RouteTableEntry(typeof(Command), lowPriorityRoute),
                });
            }, Throws.Exception);
        }

        class Command
        {
        }

        class Command2
        {
        }

        class Command3
        {
        }
    }
}