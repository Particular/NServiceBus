namespace NServiceBus.Core.Tests.Routing
{
    using NServiceBus;
    using System;
    using System.Reflection;
    using MessageNamespaceA;
    using MessageNamespaceB;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class RoutingSettingsTests
    {
        SettingsHolder settings;

        [SetUp]
        public void Setup()
        {
            settings = new SettingsHolder();
            settings.Set(new RoutingComponent.Configuration(settings));
        }

        [Test]
        public void WhenPassingTransportAddressForSenderInsteadOfEndpointName_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings(settings);
            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RouteToEndpoint(typeof(MessageWithoutNamespace), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForSenderInsteadOfEndpointName_UsingAssembly_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings(settings);
            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForSenderInsteadOfEndpointName_UsingAssemblyAndNamespace_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings(settings);
            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), nameof(MessageNamespaceA), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void WhenRoutingMessageTypeToEndpoint_ShouldConfigureMessageTypeInRoutingTable()
        {
            var routingSettings = new RoutingSettings(settings);
            routingSettings.RouteToEndpoint(typeof(SomeMessageType), "destination");

            var routingTable = ApplyConfiguredRoutes(routingSettings);
            var route = routingTable.GetRouteFor(typeof(SomeMessageType));

            Assert.That(route, Is.Not.Null);
            Assert.That(route.Endpoint, Is.EqualTo("destination"));
        }

        [Test]
        public void WhenRoutingAssemblyToEndpoint_ShouldConfigureAllContainedMessagesInRoutingTable()
        {
            var routingSettings = new RoutingSettings(settings);
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), "destination");

            var routingTable = ApplyConfiguredRoutes(routingSettings);

            var someMessageRoute = routingTable.GetRouteFor(typeof(SomeMessageType));
            var otherMessageRoute = routingTable.GetRouteFor(typeof(OtherMessageType));
            var messageWithoutNamespaceRoute = routingTable.GetRouteFor(typeof(MessageWithoutNamespace));

            Assert.That(someMessageRoute, Is.Not.Null);
            Assert.That(otherMessageRoute, Is.Not.Null);
            Assert.That(messageWithoutNamespaceRoute, Is.Not.Null);
        }

        [Test]
        public void WhenRoutingAssemblyWithNamespaceToEndpoint_ShouldOnlyConfigureMessagesWithinThatNamespace()
        {
            var routingSettings = new RoutingSettings(settings);
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), nameof(MessageNamespaceA), "destination");

            var routingTable = ApplyConfiguredRoutes(routingSettings);

            var someMessageRoute = routingTable.GetRouteFor(typeof(SomeMessageType));
            var otherMessageRoute = routingTable.GetRouteFor(typeof(OtherMessageType));
            var messageWithoutNamespaceRoute = routingTable.GetRouteFor(typeof(MessageWithoutNamespace));

            Assert.That(someMessageRoute, Is.Not.Null, "because SomeMessageType is in the given namespace");
            Assert.That(otherMessageRoute, Is.Null, "because OtherMessageType is not in the given namespace");
            Assert.That(messageWithoutNamespaceRoute, Is.Null, "because MessageWithoutNamespace is not in the given namespace");
        }

        [Theory]
        [TestCase(null)]
        [TestCase("")]
        public void WhenRoutingAssemblyWithNamespaceToEndpointAndSpecifyingEmptyNamespace_ShouldOnlyConfigureMessagesWithinEmptyNamespace(string emptyNamespace)
        {
            var routingSettings = new RoutingSettings(settings);
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), emptyNamespace, "destination");

            var routingTable = ApplyConfiguredRoutes(routingSettings);

            var someMessageRoute = routingTable.GetRouteFor(typeof(SomeMessageType));
            var otherMessageRoute = routingTable.GetRouteFor(typeof(OtherMessageType));
            var messageWithoutNamespaceRoute = routingTable.GetRouteFor(typeof(MessageWithoutNamespace));

            Assert.That(someMessageRoute, Is.Null);
            Assert.That(otherMessageRoute, Is.Null);
            Assert.That(messageWithoutNamespaceRoute, Is.Not.Null);
        }

        static UnicastRoutingTable ApplyConfiguredRoutes(RoutingSettings routingSettings)
        {
            var routingTable = new UnicastRoutingTable();
            var configuredRoutes = routingSettings.Settings.GetOrDefault<ConfiguredUnicastRoutes>();
            configuredRoutes?.Apply(routingTable, new Conventions());
            return routingTable;
        }

        string expectedExceptionMessageForWrongEndpointName = "A logical endpoint name should not contain '@', but received 'EndpointName@MyHost'. To specify an endpoint's address, use the instance mapping file for the MSMQ transport, or refer to the routing documentation.";
    }
}

namespace MessageNamespaceA
{
    using NServiceBus;
    class SomeMessageType : IMessage
    {
    }

    class YetAnotherMessageType : IMessage
    {
    }
}

namespace MessageNamespaceB
{
    using NServiceBus;
    class OtherMessageType : IMessage
    {
    }
}

class MessageWithoutNamespace : NServiceBus.IMessage
{
}