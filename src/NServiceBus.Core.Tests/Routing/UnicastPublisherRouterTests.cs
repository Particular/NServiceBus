namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Unicast.Messages;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture]
    public class UnicastPublisherRouterTests
    {
        UnicastPublishRouter router;
        MessageMetadataRegistry metadataRegistry;
        EndpointInstances endpointInstances;
        FakeSubscriptionStorage subscriptionStorage;

        [Test]
        public async Task When_subscriber_does_not_define_logical_endpoint_should_send_event_to_each_address()
        {
            subscriptionStorage.Subscribers.Add(new Subscriber("address1", null));
            subscriptionStorage.Subscribers.Add(new Subscriber("address2", null));

            var routes = await router.Route(typeof(Event), new DistributionPolicy(), new TestableOutgoingPublishContext());

            var destinations = routes.Select(ExtractDestination).ToList();
            Assert.AreEqual(2, destinations.Count);
            Assert.Contains("address1", destinations);
            Assert.Contains("address2", destinations);
        }

        [Test]
        public async Task When_multiple_subscribers_for_logical_endpoints_should_route_event_to_a_single_instance_of_each_logical_endpoint()
        {
            var sales = "Sales";
            var shipping = "Shipping";
            subscriptionStorage.Subscribers.Add(new Subscriber("sales1", sales));
            subscriptionStorage.Subscribers.Add(new Subscriber("sales2", sales));
            subscriptionStorage.Subscribers.Add(new Subscriber("shipping1", shipping));
            subscriptionStorage.Subscribers.Add(new Subscriber("shipping2", shipping));

            var routes = (await router.Route(typeof(Event), new DistributionPolicy(), new TestableOutgoingPublishContext())).ToArray();

            var destinations = routes.Select(ExtractDestination).ToList();
            Assert.AreEqual(2, destinations.Count);
            Assert.Contains("sales1", destinations);
            Assert.Contains("shipping1", destinations);
        }

        [Test]
        public async Task Should_not_route_multiple_copies_of_message_to_one_physical_destination()
        {
            subscriptionStorage.Subscribers.Add(new Subscriber("address", null));
            subscriptionStorage.Subscribers.Add(new Subscriber("address", null));
            subscriptionStorage.Subscribers.Add(new Subscriber("address", "sales"));
            subscriptionStorage.Subscribers.Add(new Subscriber("address", "sales"));
            subscriptionStorage.Subscribers.Add(new Subscriber("address", "shipping"));

            var routes = await router.Route(typeof(Event), new DistributionPolicy(), new TestableOutgoingPublishContext());

            Assert.AreEqual(1, routes.Count());
            Assert.AreEqual("address", ExtractDestination(routes.Single()));
        }

        [Test]
        public async Task Should_not_route_events_to_configured_endpoint_instances()
        {
            var logicalEndpoint = "sales";
            subscriptionStorage.Subscribers.Add(new Subscriber("address", logicalEndpoint));
            endpointInstances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(logicalEndpoint, "1"),
                new EndpointInstance(logicalEndpoint, "2")
            });

            var routes = await router.Route(typeof(Event), new DistributionPolicy(), new TestableOutgoingPublishContext());

            Assert.AreEqual(1, routes.Count());
            Assert.AreEqual("address", ExtractDestination(routes.First()));
        }

        [Test]
        public async Task Should_return_empty_list_when_no_routes_found()
        {
            var routes = await router.Route(typeof(Event), new DistributionPolicy(), new TestableOutgoingPublishContext());

            Assert.IsEmpty(routes);
        }

        static string ExtractDestination(UnicastRoutingStrategy route)
        {
            var headers = new Dictionary<string, string>();
            var addressTag = (UnicastAddressTag)route.Apply(headers);
            return addressTag.Destination;
        }

        [SetUp]
        public void Setup()
        {
            metadataRegistry = new MessageMetadataRegistry(_ => true);
            endpointInstances = new EndpointInstances();
            subscriptionStorage = new FakeSubscriptionStorage();
            router = new UnicastPublishRouter(
                metadataRegistry,
                i => string.Empty,
                subscriptionStorage);
        }

        class FakeSubscriptionStorage : ISubscriptionStorage
        {
            public List<Subscriber> Subscribers { get; } = new List<Subscriber>();
            public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
            {
                throw new NotImplementedException();
            }

            public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
            {
                return Task.FromResult<IEnumerable<Subscriber>>(Subscribers);
            }
        }

        class Event : IEvent
        {
        }
    }
}