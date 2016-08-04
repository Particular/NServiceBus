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
        public async Task When_overriding_static_routes_should_use_route_which_overrides_previous_routes()
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
        public async Task When_static_rule_found_should_not_execute_fallback()
        {
            var routingTable = new UnicastRoutingTable();
            var staticRoute = UnicastRoute.CreateFromPhysicalAddress("somewhere");
            var executedFallbackRoute = false;
            routingTable.RouteTo(typeof(Command), staticRoute);
            routingTable.SetFallbackRoute((t, c) =>
            {
                executedFallbackRoute = true;
                return Task.FromResult<IUnicastRoute>(null);
            });

            var route = await routingTable.GetRouteFor(typeof(Command), new ContextBag());

            Assert.That(route, Is.EqualTo(staticRoute));
            Assert.That(executedFallbackRoute, Is.False);
        }

        [Test]
        public async Task When_no_static_rule_found_should_execute_fallback()
        {
            var routingTable = new UnicastRoutingTable();
            var dynamicRoute = UnicastRoute.CreateFromPhysicalAddress("somewhere");
            routingTable.SetFallbackRoute((t, c) => Task.FromResult<IUnicastRoute>(dynamicRoute));

            var route = await routingTable.GetRouteFor(typeof(Command), new ContextBag());

            Assert.That(route, Is.EqualTo(dynamicRoute));
        }

        [Test]
        public async Task When_no_static_rule_found_and_no_fallback_configured_should_return_null()
        {
            var routingTable = new UnicastRoutingTable();

            var route = await routingTable.GetRouteFor(typeof(Command), new ContextBag());

            Assert.That(route, Is.Null);
        }

        [Test]
        public void When_overriding_route_fallback_should_throw_exception()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.SetFallbackRoute((t, c) => Task.FromResult<IUnicastRoute>(null));

            Assert.That(() => routingTable.SetFallbackRoute((t, c) => Task.FromResult<IUnicastRoute>(null)), Throws.Exception.TypeOf<Exception>().And.Message.Contains("A custom fallback route has already been configured. Only one fallback route is supported."));
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