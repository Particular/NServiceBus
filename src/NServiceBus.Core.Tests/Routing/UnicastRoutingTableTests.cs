namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

            var routes = await routingTable.GetDestinationsFor(typeof(Command), new ContextBag());

            Assert.That(routes, Has.Count.EqualTo(1));
            Assert.That(routes.Single(), Is.EqualTo(expectedRoute));
        }

        [Test]
        public void When_returning_multiple_dynamic_routes_for_same_type_should_throw_exceptions()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.AddDynamic((t, c) => new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("a")
            });
            routingTable.AddDynamic((t, c) => new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("b")
            });
            routingTable.AddDynamic((t, c) => Task.FromResult<IEnumerable<IUnicastRoute>>(new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("c")
            }));

            var exception = Assert.ThrowsAsync<Exception>(() => routingTable.GetDestinationsFor(typeof(Command), new ContextBag()));
            Assert.That(exception.Message, Does.Contain($"Found ambiguous routes for message '{nameof(Command)}'. Check your dynamic and static routes and avoid multiple routes for the same message type."));
        }

        [Test]
        public void When_static_and_dynamic_routes_found_for_same_type_should_throw_exception()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.RouteTo(typeof(Command), UnicastRoute.CreateFromEndpointName("a"));
            routingTable.AddDynamic((t, c) => new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("b")
            });
            routingTable.AddDynamic((t, c) => Task.FromResult<IEnumerable<IUnicastRoute>>(new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("c")
            }));

            var exception = Assert.ThrowsAsync<Exception>(() => routingTable.GetDestinationsFor(typeof(Command), new ContextBag()));
            Assert.That(exception.Message, Does.Contain($"Found ambiguous routes for message '{nameof(Command)}'. Check your dynamic and static routes and avoid multiple routes for the same message type."));
        }

        class Command
        {
        }
    }
}