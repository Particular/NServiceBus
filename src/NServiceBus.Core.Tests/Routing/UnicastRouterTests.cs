﻿namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using Unicast.Messages;
    using NUnit.Framework;

    [TestFixture]
    public class UnicastRouterTests
    {
        UnicastRouter router;
        MessageMetadataRegistry metadataRegistry;
        UnicastRoutingTable routingTable;
        EndpointInstances endpointInstances;
        TransportAddresses transportAddresses;

        [Test]
        public void Should_route_a_command_to_a_single_non_scaled_out_destination()
        {
            var sales = "Sales";
            metadataRegistry.RegisterMessageType(typeof(Command));
            routingTable.RouteToEndpoint(typeof(Command), sales);
            endpointInstances.Add(new EndpointInstance(sales, null, null));
            transportAddresses.AddRule(i => i.ToString());

            var routes = router.Route(typeof(Command), new SingleInstanceRoundRobinDistributionStrategy(), new ContextBag()).Result.ToArray();

            Assert.AreEqual(1, routes.Length);
            var headers = new Dictionary<string, string>();
            var addressTag = (UnicastAddressTag) routes[0].Apply(headers);
            Assert.AreEqual("Sales", addressTag.Destination);
        }

        [Test]
        public void Should_route_an_event_to_a_single_non_scaled_out_destination()
        {
            var sales = "Sales";
            metadataRegistry.RegisterMessageType(typeof(Event));
            routingTable.RouteToEndpoint(typeof(Event), sales);
            endpointInstances.Add(new EndpointInstance(sales));
            transportAddresses.AddRule(i => i.ToString());

            var routes = router.Route(typeof(Event), new SingleInstanceRoundRobinDistributionStrategy(), new ContextBag()).Result.ToArray();

            Assert.AreEqual(1, routes.Length);
            Assert.AreEqual("Sales", ExtractDestination(routes[0]));
        }

        [Test]
        public void Should_route_an_event_to_a_single_instance_of_each_endpoint()
        {
            var sales = "Sales";
            var shipping = "Shipping";
            metadataRegistry.RegisterMessageType(typeof(Event));
            routingTable.RouteToEndpoint(typeof(Event), sales);
            routingTable.RouteToEndpoint(typeof(Event), shipping);

            endpointInstances.Add(new EndpointInstance(sales, "1"));
            endpointInstances.AddDynamic(e => Task.FromResult(EnumerableEx.Single(new EndpointInstance(sales, "2"))));
            endpointInstances.Add(new EndpointInstance(shipping, "1", null), new EndpointInstance(shipping, "2"));

            transportAddresses.AddRule(i => i.ToString());

            var routes = router.Route(typeof(Event), new SingleInstanceRoundRobinDistributionStrategy(), new ContextBag()).Result.ToArray();

            Assert.AreEqual(2, routes.Length);
            Assert.AreEqual("Sales-1", ExtractDestination(routes[0]));
            Assert.AreEqual("Shipping-1", ExtractDestination(routes[1]));
        }

        [Test]
        public void Should_not_send_multiple_copies_of_message_to_one_physical_destination()
        {
            var sales = "Sales";
            metadataRegistry.RegisterMessageType(typeof(Event));

            routingTable.RouteToEndpoint(typeof(Event), sales);
            routingTable.RouteToAddress(typeof(Event), "Sales-1");
            endpointInstances.Add(new EndpointInstance(sales, "1"));
            transportAddresses.AddRule(i => i.ToString());

            var routes = router.Route(typeof(Event), new SingleInstanceRoundRobinDistributionStrategy(), new ContextBag()).Result.ToArray();

            Assert.AreEqual(1, routes.Length);
        }

        [Test]
        public void Should_not_pass_duplicate_routes_to_distribution_strategy()
        {
            var sales = "Sales";
            metadataRegistry.RegisterMessageType(typeof(Event));

            routingTable.RouteToEndpoint(typeof(Event), sales);
            endpointInstances.Add(new EndpointInstance(sales, "1"));
            endpointInstances.AddDynamic(name =>
            {
                IEnumerable<EndpointInstance> results = new[]
                {
                    new EndpointInstance(sales, "1")
                };
                return Task.FromResult(results);
            });
            transportAddresses.AddRule(i => i.ToString());

            var routes = router.Route(typeof(Event), new TestDistributionStrategy(), new ContextBag()).Result.ToArray();

            Assert.AreEqual(1, routes.Length);
        }

        [Test]
        public void Should_return_empty_list_when_no_routes_found()
        {
            var routes = router.Route(typeof(Event), new TestDistributionStrategy(), new ContextBag()).Result.ToArray();

            Assert.IsEmpty(routes);
        }

        class TestDistributionStrategy : DistributionStrategy
        {
            public override IEnumerable<UnicastRoutingTarget> SelectDestination(IList<UnicastRoutingTarget> allInstances)
            {
                Assert.AreEqual(1, allInstances.Count);
                return allInstances;
            }
        }

        static string ExtractDestination(UnicastRoutingStrategy route)
        {
            var headers = new Dictionary<string, string>();
            var addressTag = (UnicastAddressTag) route.Apply(headers);
            var destination = addressTag.Destination;
            return destination;
        }

        [SetUp]
        public void Setup()
        {
            metadataRegistry = new MessageMetadataRegistry(new Conventions());
            routingTable = new UnicastRoutingTable();
            endpointInstances = new EndpointInstances();
            transportAddresses = new TransportAddresses(address => null);
            router = new UnicastSendRouter(
                metadataRegistry,
                routingTable,
                endpointInstances,
                transportAddresses);
        }

        class Command : ICommand
        {
        }

        class Event : IEvent
        {
        }
    }
}