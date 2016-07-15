﻿namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Extensibility;
    using MessageNamespaceA;
    using MessageNamespaceB;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class RoutingSettingsTests
    {
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

            Assert.That(routes.Count, Is.EqualTo(1));
            Assert.That(routes.Single().Endpoint, Is.EqualTo("destination"));
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

            Assert.That(routes.Count, Is.EqualTo(3));
            Assert.That(routes, Has.All.Property("Endpoint").EqualTo("destination"));
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

            Assert.That(result1.Count, Is.EqualTo(1), "because SomeMessageType is in the given namespace");
            Assert.That(result2.Count, Is.EqualTo(0), "because none of the messages are in the given namespace");
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

            Assert.That(result1.Count, Is.EqualTo(1));
            Assert.That(result2.Count, Is.EqualTo(0));
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