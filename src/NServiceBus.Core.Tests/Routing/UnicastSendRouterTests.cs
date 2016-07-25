namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using Unicast.Messages;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class UnicastSendRouterTests
    {
        UnicastSendRouter router;
        MessageMetadataRegistry metadataRegistry;
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
        public async Task When_multiple_instances_for_logical_endpoints_should_route_message_to_a_single_instance_of_each_logical_endpoint()
        {
            var sales = "Sales";
            var shipping = "Shipping";
            routingTable.RouteTo(typeof(Command), UnicastRoute.CreateFromEndpointName(sales));
            routingTable.RouteTo(typeof(Command), UnicastRoute.CreateFromEndpointName(shipping));

            endpointInstances.Add(new EndpointInstance(sales, "1"));
            endpointInstances.AddDynamic(e => Task.FromResult(EnumerableEx.Single(new EndpointInstance(sales, "2"))));
            endpointInstances.Add(new EndpointInstance(shipping, "1", null), new EndpointInstance(shipping, "2"));

            var routes = (await router.Route(typeof(Command), new DistributionPolicy(), new ContextBag())).ToArray();

            Assert.AreEqual(2, routes.Length);
            Assert.AreEqual("Sales-1", ExtractDestination(routes[0]));
            Assert.AreEqual("Shipping-1", ExtractDestination(routes[1]));
        }

        [Test]
        public async Task Should_not_route_multiple_copies_of_message_to_one_physical_destination()
        {
            var sales = "Sales";
            routingTable.RouteTo(typeof(Command), UnicastRoute.CreateFromEndpointName(sales));
            endpointInstances.Add(new EndpointInstance(sales, "1"));
            routingTable.RouteTo(typeof(Command), UnicastRoute.CreateFromPhysicalAddress(sales + "-1"));

            var routes = await router.Route(typeof(Command), new DistributionPolicy(), new ContextBag());

            Assert.AreEqual(1, routes.Count());
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
            metadataRegistry = new MessageMetadataRegistry(new Conventions());
            routingTable = new UnicastRoutingTable();
            endpointInstances = new EndpointInstances();
            router = new UnicastSendRouter(
                metadataRegistry,
                routingTable,
                endpointInstances,
                i => i.ToString());
        }

        class Command : ICommand
        {
        }
    }
}