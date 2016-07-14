
using NServiceBus;

namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Configuration.AdvanceExtensibility;
    using Extensibility;
    using MessageNamespaceA;
    using MessageNamespaceB;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class RoutingSettingsTests
    {
        public void When_passing_endpoint_name_for_sender_route_added_to_unicast_routing_table()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            routingSettings.RouteToEndpoint(typeof(Message), "EndpointName");

            var table = routingSettings.GetSettings().Get<UnicastRoutingTable>();

            var routesForMessage = table.GetDestinationsFor(new List<Type> { typeof(Message) }, null).Result.Count();
            Assert.AreEqual(routesForMessage, 1);
        }

        [Test]
        public void When_passing_transport_address_for_sender_throws_exception()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());

            Assert.Throws(typeof(ArgumentException),
                            () => routingSettings.RouteToEndpoint(typeof(Message), "EndpointName@MyHost"),
                            "Expected an endpoint name but received 'EndpointName@MyHost'.");
        }

        [Test]
        public void When_passing_endpoint_name_for_publisher_route_added_to_unicast_routing_table()
        {
            var routingSettings = new RoutingSettings<MsmqTransport>(new SettingsHolder());
            routingSettings.RegisterPublisherForType(typeof(Event), "EndpointName");

            var publishers = routingSettings.GetSettings().Get<Publishers>();

            var publishersForEvent = publishers.GetPublisherFor(typeof(Event));
            Assert.AreEqual(publishersForEvent.Count(), 1);
        }

        [Test]
        public void When_passing_transport_address_for_publisher_throws_exception()
        {
            var routingSettings = new RoutingSettings<MsmqTransport>(new SettingsHolder());

            Assert.Throws(typeof(ArgumentException),
                            () => routingSettings.RegisterPublisherForType(typeof(Event), "EndpointName@MyHost"),
                            "Expected an endpoint name but received 'EndpointName@MyHost'.");
        }

        [Test]
        public async Task WhenRoutingMessageTypeToEndpoint_ShouldConfigureMessageTypeInRoutingTable()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());

            routingSettings.RouteToEndpoint(typeof(SomeMessageType), "destination");

            var routingTable = routingSettings.Settings.Get<UnicastRoutingTable>();
            var routes = await routingTable.GetDestinationsFor(new[]
            {
                typeof(SomeMessageType)
            }, new ContextBag());
            var routingTargets = await RetrieveRoutingTargets(routes);

            Assert.That(routes.Count(), Is.EqualTo(1));
            Assert.That(routingTargets.Single().Endpoint, Is.EqualTo("destination"));
        }

        [Test]
        public async Task WhenRoutingAssemblyToEndpoint_ShouldConfigureAllContainedMessagesInRoutingTable()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), "destination");
            var routingTable = routingSettings.Settings.Get<UnicastRoutingTable>();

            var routes = await routingTable.GetDestinationsFor(new []
            {
                typeof(SomeMessageType),
                typeof(OtherMessageType),
                typeof(MessageWithoutNamespace)
            }, new ContextBag());
            var routingTarget = await RetrieveRoutingTargets(routes);

            Assert.That(routes.Count(), Is.EqualTo(3));
            Assert.That(routingTarget, Has.All.Property("Endpoint").EqualTo("destination"));
        }

        [Test]
        public async Task WhenRoutingAssemblyWithNamespaceToEndpoint_ShouldOnlyConfigureMessagesWithinThatNamespace()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), nameof(MessageNamespaceA), "destination");
            var routingTable = routingSettings.Settings.Get<UnicastRoutingTable>();

            var result1 = await routingTable.GetDestinationsFor(new[]
            {
                typeof(SomeMessageType)
            }, new ContextBag());

            var result2 = await routingTable.GetDestinationsFor(new[]
            {
                typeof(OtherMessageType),
                typeof(MessageWithoutNamespace)
            }, new ContextBag());

            Assert.That(result1.Count(), Is.EqualTo(1), "because SomeMessageType is in the given namespace");
            Assert.That(result2.Count(), Is.EqualTo(0), "because none of the messages are in the given namespace");
        }

        [Theory]
        [TestCase(null)]
        [TestCase("")]
        public async Task WhenRoutingAssemblyWithNamespaceToEndpointAndSpecifyingEmptyNamespace_ShouldOnlyConfigureMessagesWithinEmptyNamespace(string emptyNamespace)
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), emptyNamespace, "destination");
            var routingTable = routingSettings.Settings.Get<UnicastRoutingTable>();

            var result1 = await routingTable.GetDestinationsFor(new[]
            {
                typeof(MessageWithoutNamespace)
            }, new ContextBag());
            var result2 = await routingTable.GetDestinationsFor(new[]
            {
                typeof(SomeMessageType),
                typeof(OtherMessageType)
            }, new ContextBag());

            Assert.That(result1.Count(), Is.EqualTo(1));
            Assert.That(result2.Count(), Is.EqualTo(0));
        }

        static async Task<IEnumerable<UnicastRoutingTarget>> RetrieveRoutingTargets(IEnumerable<IUnicastRoute> result)
        {
            return (await Task.WhenAll(result.Select(x => x.Resolve(e => Task.FromResult<IEnumerable<EndpointInstance>>(new[]
            {
                new EndpointInstance(e)
            }))))).SelectMany(x => x);
        }
    }
}

namespace MessageNamespaceA
{
    class SomeMessageType
    {
    }
}

namespace MessageNamespaceB
{
    class OtherMessageType
    {
    }
}

class MessageWithoutNamespace
{
}

class Message : IMessage { }

class Event : IEvent { }
