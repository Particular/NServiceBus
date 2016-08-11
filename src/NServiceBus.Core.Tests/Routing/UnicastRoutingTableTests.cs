﻿namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using ApprovalTests;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    class UnicastRoutingTableTests
    {
        [Test]
        public void When_group_does_not_exist_routes_are_added()
        {
            var routingTable = new UnicastRoutingTable();
            var route = UnicastRoute.CreateFromEndpointName("Endpoint1");
            routingTable.AddOrReplaceRoutes("key", new List<RouteTableEntry>()
            {
                new RouteTableEntry(typeof(Command), route),
            });

            var retrievedRoute = routingTable.GetRouteFor(typeof(Command));
            Assert.AreSame(route, retrievedRoute);
        }

        [Test]
        public void When_group_exists_routes_are_replaced()
        {
            var routingTable = new UnicastRoutingTable();
            var oldRoute = UnicastRoute.CreateFromEndpointName("Endpoint1");
            var newRoute = UnicastRoute.CreateFromEndpointName("Endpoint2");
            routingTable.AddOrReplaceRoutes("key", new List<RouteTableEntry>()
            {
                new RouteTableEntry(typeof(Command), oldRoute),
            });

            routingTable.AddOrReplaceRoutes("key", new List<RouteTableEntry>()
            {
                new RouteTableEntry(typeof(Command), newRoute),
            });

            var retrievedRoute = routingTable.GetRouteFor(typeof(Command));
            Assert.AreSame(newRoute, retrievedRoute);
        }

        [Test]
        public void When_routes_are_ambiguous_it_throws_exception()
        {
            var routingTable = new UnicastRoutingTable();
            var lowPriorityRoute = UnicastRoute.CreateFromEndpointName("Endpoint1");
            var highPriorityRoute = UnicastRoute.CreateFromEndpointName("Endpoint2");

            routingTable.AddOrReplaceRoutes("key2", new List<RouteTableEntry>()
            {
                new RouteTableEntry(typeof(Command), highPriorityRoute),
            });

            Assert.That(() =>
            {
                routingTable.AddOrReplaceRoutes("key1", new List<RouteTableEntry>()
                {
                    new RouteTableEntry(typeof(Command), lowPriorityRoute),
                });
            }, Throws.Exception);
        }

        [Test]
        public void Should_log_changes()
        {
            var routingTable = new UnicastRoutingTable();
            var log = "";
            routingTable.SetLogChangeAction(x => { log = x; });
            routingTable.AddOrReplaceRoutes("key", new List<RouteTableEntry>
            {
                new RouteTableEntry(typeof(Command), UnicastRoute.CreateFromEndpointName("Endpoint")),
                new RouteTableEntry(typeof(Command2), UnicastRoute.CreateFromEndpointInstance(new EndpointInstance("Endpoint", "XYZ"))),
                new RouteTableEntry(typeof(Command3), UnicastRoute.CreateFromPhysicalAddress("Endpoint@Machine")),
            });
            Approvals.Verify(log);
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