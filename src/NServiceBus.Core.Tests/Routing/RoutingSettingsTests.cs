﻿namespace NServiceBus.Core.Tests.Routing
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
    using Unicast.Messages;

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

            var routingTable = ApplyConfiguredRoutes(routingSettings);

            var routes = await routingTable.GetDestinationsFor(new[]
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

            var routingTable = ApplyConfiguredRoutes(routingSettings);

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

            var routingTable = ApplyConfiguredRoutes(routingSettings);

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

        static UnicastRoutingTable ApplyConfiguredRoutes(RoutingSettings routingSettings)
        {
            var routingTable = new UnicastRoutingTable();
            foreach (var registration in routingSettings.Settings.Get<ConfiguredUnicastRoutes>())
            {
                registration(routingTable, Assembly.GetExecutingAssembly().GetTypes(), new MessageMetadataRegistry(new Conventions()));
            }
            return routingTable;
        }

        static async Task<IEnumerable<UnicastRoutingTarget>> RetrieveRoutingTargets(IEnumerable<IUnicastRoute> result)
        {
            return (await Task.WhenAll(result.Select(x => x.Resolve(e => Task.FromResult<IEnumerable<EndpointInstance>>(new[]
            {
                new EndpointInstance(e)
            }))))).SelectMany(x => x);
        }

        string expectedExceptionMessageForWrongEndpointName = "A logical endpoint name should not contain '@', but received 'EndpointName@MyHost'. To specify an endpoint's address, use the instance mapping file for the MSMQ transport, or refer to the routing documentation.";
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