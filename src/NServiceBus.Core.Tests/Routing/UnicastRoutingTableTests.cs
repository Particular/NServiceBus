namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    class UnicastRoutingTableTests
    {
        [Test]
        public void When_registering_multiple_static_routes_for_same_type_should_throw_exception()
        {
            var routingTable = new UnicastRoutingTable();
            var firstRoute = UnicastRoute.CreateFromPhysicalAddress("address@somewhere");
            var secondRoute = UnicastRoute.CreateFromEndpointName("sales");

            routingTable.RouteTo(typeof(Command), firstRoute);

            Assert.That(() => routingTable.RouteTo(typeof(Command), secondRoute), Throws.Exception.TypeOf<Exception>().And.Message.Contains($"The static routing table already contains a route for message '{nameof(Command)}'. Remove the ambiguous route registrations or override the existing route."));
        }

        [Test]
        public async Task When_overriding_static_routes_with_same_type_should_only_use_last_route()
        {
            var routingTable = new UnicastRoutingTable();
            var expectedRoute = UnicastRoute.CreateFromEndpointName("sales");
            routingTable.RouteTo(typeof(Command), UnicastRoute.CreateFromPhysicalAddress("address@somewhere"), true);
            routingTable.RouteTo(typeof(Command), UnicastRoute.CreateFromEndpointInstance(new EndpointInstance("billing")), true);
            routingTable.RouteTo(typeof(Command), expectedRoute, true);

            var route = await routingTable.GetRouteFor(typeof(Command), new ContextBag());

            Assert.That(route, Is.EqualTo(expectedRoute));
        }

        [Test]
        public async Task When_static_rule_matches_should_not_execute_dynamic_rules()
        {
            var routingTable = new UnicastRoutingTable();
            var staticRoute = UnicastRoute.CreateFromPhysicalAddress("somewhere");
            routingTable.RouteTo(typeof(Command), staticRoute);
            routingTable.AddDynamic((t,c) => ThrowSync());
            routingTable.AddDynamic((t,c) => Task.FromResult(ThrowSync()));

            var route = await routingTable.GetRouteFor(typeof(Command), new ContextBag());

            Assert.That(route, Is.EqualTo(staticRoute));
        }

        [Test]
        public async Task When_returning_dynamic_routes_for_same_type_return_route_from_first_matching_rule()
        {
            var routingTable = new UnicastRoutingTable();
            var firstMatch = UnicastRoute.CreateFromPhysicalAddress("a");
            routingTable.AddDynamic((t, c) => (IUnicastRoute)null);
            routingTable.AddDynamic((t, c) => firstMatch);
            routingTable.AddDynamic((t, c) => UnicastRoute.CreateFromPhysicalAddress("c"));

            var route = await routingTable.GetRouteFor(typeof(Command), new ContextBag());

            Assert.That(route, Is.EqualTo(firstMatch));
        }

        [Test]
        public async Task When_matching_dynamic_route_should_not_execute_async_dynamic_routes()
        {
            var routingTable = new UnicastRoutingTable();
            var syncRoute = UnicastRoute.CreateFromPhysicalAddress("a");
            routingTable.AddDynamic((t, c) => Task.FromResult(ThrowSync()));
            routingTable.AddDynamic((t, c) => syncRoute);
            routingTable.AddDynamic((t, c) => Task.FromResult(ThrowSync()));

            var route = await routingTable.GetRouteFor(typeof(Command), new ContextBag());

            Assert.That(route, Is.EqualTo(syncRoute));
        }

        [Test]
        public async Task When_multiple_async_dynamic_routes_for_same_type_return_route_from_first_matching_rule()
        {
            var routingTable = new UnicastRoutingTable();
            var firstMatch = UnicastRoute.CreateFromPhysicalAddress("a");
            routingTable.AddDynamic((t, c) => Task.FromResult<IUnicastRoute>(null));
            routingTable.AddDynamic((t, c) => Task.FromResult<IUnicastRoute>(firstMatch));
            routingTable.AddDynamic((t, c) => Task.FromResult<IUnicastRoute>(UnicastRoute.CreateFromPhysicalAddress("c")));

            var route = await routingTable.GetRouteFor(typeof(Command), new ContextBag());

            Assert.That(route, Is.EqualTo(firstMatch));
        }

        static IUnicastRoute ThrowSync()
        {
            throw new Exception();
        }

        class Command
        {
        }
    }
}