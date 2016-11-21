namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class UnicastPublisherRouterTests
    {
        UnicastPublishRouter router;
        EndpointInstances endpointInstances;
        UnicastSubscriberTable subscriberTable;

        [Test]
        public void When_subscriber_does_not_define_logical_endpoint_should_send_event_to_each_address()
        {
            AddRoutes(
                UnicastRoute.CreateFromPhysicalAddress("address1"),
                UnicastRoute.CreateFromPhysicalAddress("address2")
                );

            var routes = router.Route(typeof(Event), new DistributionPolicy(), new ContextBag());

            var destinations = routes.Select(ExtractDestination).ToList();
            Assert.AreEqual(2, destinations.Count);
            Assert.Contains("address1", destinations);
            Assert.Contains("address2", destinations);
        }

        
        [Test]
        public void When_multiple_subscribers_for_logical_endpoints_should_route_event_to_a_single_instance_of_each_logical_endpoint()
        {
            var sales = "Sales";
            var shipping = "Shipping";

            AddRoutes(
                UnicastRoute.CreateFromPhysicalAddress("sales1", sales),
                UnicastRoute.CreateFromPhysicalAddress("sales2", sales),
                UnicastRoute.CreateFromPhysicalAddress("shipping1", shipping),
                UnicastRoute.CreateFromPhysicalAddress("shipping2", shipping));

            var routes = router.Route(typeof(Event), new DistributionPolicy(), new ContextBag()).ToArray();

            var destinations = routes.Select(ExtractDestination).ToList();
            Assert.AreEqual(2, destinations.Count);
            Assert.Contains("sales1", destinations);
            Assert.Contains("shipping1", destinations);
        }

        [Test]
        public void Should_not_route_multiple_copies_of_message_to_one_physical_destination()
        {
            AddRoutes(
                UnicastRoute.CreateFromPhysicalAddress("address"),
                UnicastRoute.CreateFromPhysicalAddress("address"),
                UnicastRoute.CreateFromPhysicalAddress("address", "sales"),
                UnicastRoute.CreateFromPhysicalAddress("address", "sales"),
                UnicastRoute.CreateFromPhysicalAddress("address", "shipping"));

            var routes = router.Route(typeof(Event), new DistributionPolicy(), new ContextBag());

            Assert.AreEqual(1, routes.Count());
            Assert.AreEqual("address", ExtractDestination(routes.Single()));
        }

        [Test]
        public void Should_not_route_events_to_configured_endpoint_instances()
        {
            var logicalEndpoint = "sales";

            AddRoutes(UnicastRoute.CreateFromPhysicalAddress("address", logicalEndpoint));

            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(logicalEndpoint, "1"),
                new EndpointInstance(logicalEndpoint, "2")
            });

            var routes = router.Route(typeof(Event), new DistributionPolicy(), new ContextBag()).ToArray();

            Assert.AreEqual(1, routes.Count());
            Assert.AreEqual("address", ExtractDestination(routes.First()));
        }

        [Test]
        public void Should_return_empty_list_when_no_routes_found()
        {
           var routes = router.Route(typeof(Event), new DistributionPolicy(), new ContextBag());

            Assert.IsEmpty(routes);
        }

        static string ExtractDestination(UnicastRoutingStrategy route)
        {
            var headers = new Dictionary<string, string>();
            var addressTag = (UnicastAddressTag)route.Apply(headers);
            return addressTag.Destination;
        }

        void AddRoutes(params UnicastRoute[] routes)
        {
            subscriberTable.AddOrReplaceRoutes(typeof(Event).FullName, routes.Select(r => new RouteTableEntry(typeof(Event), r)).ToList());
        }

        [SetUp]
        public void Setup()
        {
            endpointInstances = new EndpointInstances();
            subscriberTable = new UnicastSubscriberTable();
            router = new UnicastPublishRouter(subscriberTable, endpointInstances, i => i.ToString());
        }
        
        class Event : IEvent
        {
        }
    }
}