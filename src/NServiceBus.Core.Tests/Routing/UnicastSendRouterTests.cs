namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class UnicastSendRouterTests
    {
        UnicastSendRouter router;
        UnicastRoutingTable routingTable;
        EndpointInstances endpointInstances;

        [Test]
        public async Task When_routing_command_to_logical_endpoint_without_configured_instances_should_route_to_a_single_destination()
        {
            var logicalEndpointName = "Sales";
            routingTable.RouteTo(typeof(Command), UnicastRoute.CreateFromEndpointName(logicalEndpointName));

            var routes = await router.Route(typeof(Command), new DistributionPolicy(), new ContextBag());

            Assert.AreEqual(1, routes.Count());
            Assert.AreEqual(logicalEndpointName, ExtractDestination(routes.First()));
        }

        [Test]
        public async Task When_multiple_dynamic_instances_for_logical_endpoints_should_route_message_to_a_single_instance()
        {
            var sales = "Sales";
            routingTable.AddDynamic((t, c) => UnicastRoute.CreateFromEndpointName(sales));

            endpointInstances.Add(new EndpointInstance(sales, "1"));
            endpointInstances.AddDynamic(e => Task.FromResult(EnumerableEx.Single(new EndpointInstance(sales, "2"))));

            var routes = (await router.Route(typeof(Command), new DistributionPolicy(), new ContextBag())).ToArray();

            Assert.AreEqual(1, routes.Length);
            Assert.AreEqual("Sales-1", ExtractDestination(routes[0]));
        }

        [Test]
        public async Task Should_return_empty_list_when_no_routes_found()
        {
            var routes = await router.Route(typeof(Command), new DistributionPolicy(), new ContextBag());

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