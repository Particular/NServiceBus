namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Extensibility;
    using MessageNamespaceA;
    using MessageNamespaceB;
    using NServiceBus.Features;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Settings;

    [TestFixture]
    public class RoutingSettingsTests
    {
        [Test]
        public void WhenPassingTransportAddressForSenderInsteadOfEndpointName_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RouteToEndpoint(typeof(MessageWithoutNamespace), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForSenderInsteadOfEndpointName_UsingAssembly_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void WhenPassingTransportAddressForSenderInsteadOfEndpointName_UsingAssemblyAndNamespace_ShouldThrowException()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            var expectedExceptionMessage = expectedExceptionMessageForWrongEndpointName;

            var exception = Assert.Throws<ArgumentException>(() => routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), nameof(MessageNamespaceA), "EndpointName@MyHost"));
            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public async Task WhenRoutingMessageTypeToEndpoint_ShouldConfigureMessageTypeInRoutingTable()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            routingSettings.RouteToEndpoint(typeof(SomeMessageType), "destination");

            var routingTable = ApplyConfiguredRoutes(routingSettings);
            var route = await routingTable.GetRouteFor(typeof(SomeMessageType), new ContextBag());
            var routingTargets = await RetrieveRoutingTargets(route);

            Assert.That(route, Is.Not.Null);
            Assert.That(routingTargets.Single().Endpoint, Is.EqualTo("destination"));
        }

        [Test]
        public async Task WhenRoutingAssemblyToEndpoint_ShouldConfigureAllContainedMessagesInRoutingTable()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), "destination");

            var routingTable = ApplyConfiguredRoutes(routingSettings);

            var someMessageRoute = await routingTable.GetRouteFor(typeof(SomeMessageType), new ContextBag());
            var otherMessageRoute = await routingTable.GetRouteFor(typeof(OtherMessageType), new ContextBag());
            var messageWithoutNamespaceRoute = await routingTable.GetRouteFor(typeof(MessageWithoutNamespace), new ContextBag());

            Assert.That(someMessageRoute, Is.Not.Null);
            Assert.That(otherMessageRoute, Is.Not.Null);
            Assert.That(messageWithoutNamespaceRoute, Is.Not.Null);
        }

        [Test]
        public async Task WhenRoutingAssemblyWithNamespaceToEndpoint_ShouldOnlyConfigureMessagesWithinThatNamespace()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), nameof(MessageNamespaceA), "destination");

            var routingTable = ApplyConfiguredRoutes(routingSettings);

            var someMessageRoute = await routingTable.GetRouteFor(typeof(SomeMessageType), new ContextBag());
            var otherMessageRoute = await routingTable.GetRouteFor(typeof(OtherMessageType), new ContextBag());
            var messageWithoutNamespaceRoute = await routingTable.GetRouteFor(typeof(MessageWithoutNamespace), new ContextBag());

            Assert.That(someMessageRoute, Is.Not.Null, "because SomeMessageType is in the given namespace");
            Assert.That(otherMessageRoute, Is.Null, "because OtherMessageType is not in the given namespace");
            Assert.That(messageWithoutNamespaceRoute, Is.Null, "because MessageWithoutNamespace is not in the given namespace");
        }

        [Theory]
        [TestCase(null)]
        [TestCase("")]
        public async Task WhenRoutingAssemblyWithNamespaceToEndpointAndSpecifyingEmptyNamespace_ShouldOnlyConfigureMessagesWithinEmptyNamespace(string emptyNamespace)
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());
            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), emptyNamespace, "destination");

            var routingTable = ApplyConfiguredRoutes(routingSettings);

            var someMessageRoute = await routingTable.GetRouteFor(typeof(SomeMessageType), new ContextBag());
            var otherMessageRoute = await routingTable.GetRouteFor(typeof(OtherMessageType), new ContextBag());
            var messageWithoutNamespaceRoute = await routingTable.GetRouteFor(typeof(MessageWithoutNamespace), new ContextBag());

            Assert.That(someMessageRoute, Is.Null);
            Assert.That(otherMessageRoute, Is.Null);
            Assert.That(messageWithoutNamespaceRoute, Is.Not.Null);
        }

        static UnicastRoutingTable ApplyConfiguredRoutes(RoutingSettings routingSettings)
        {
            var routingTable = new UnicastRoutingTable();
            var conventions = new Conventions
            {
                IsMessageTypeAction = type => true
            };

            foreach (var registration in routingSettings.Settings.Get<ConfiguredUnicastRoutes>())
            {
                registration(routingTable, conventions);
            }
            return routingTable;
        }

        static Task<IEnumerable<UnicastRoutingTarget>> RetrieveRoutingTargets(IUnicastRoute result)
        {
            return result.Resolve(e => Task.FromResult<IEnumerable<EndpointInstance>>(new[]
            {
                new EndpointInstance(e)
            }));
        }

        string expectedExceptionMessageForWrongEndpointName = "A logical endpoint name should not contain '@', but received 'EndpointName@MyHost'. To specify an endpoint's address, use the instance mapping file for the MSMQ transport, or refer to the routing documentation.";
    }
}

namespace MessageNamespaceA
{
    class SomeMessageType
    {
    }

    class YetAnotherMessageType
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