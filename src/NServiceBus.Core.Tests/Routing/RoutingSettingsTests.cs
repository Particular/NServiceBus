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
            var result = await routingTable.GetDestinationsFor(new List<Type>
            {
                typeof(SomeMessageType)
            }, new ContextBag());

            Assert.That(result.Count(), Is.EqualTo(1));
            var routingTargets = await RetrieveRoutingTargets(result);
            Assert.That(routingTargets.Single().Endpoint, Is.EqualTo("destination"));
        }

        [Test]
        public async Task WhenRoutingAssemblyToEndpoint_ShouldConfigureAllContainedMessagesInRoutingTable()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());

            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), "destination");

            var routingTable = routingSettings.Settings.Get<UnicastRoutingTable>();
            var result = await routingTable.GetDestinationsFor(new List<Type>
            {
                typeof(SomeMessageType),
                typeof(OtherMessageType),
                typeof(MessageWithoutNamespace)
            }, new ContextBag());

            Assert.That(result.Count(), Is.EqualTo(3));
            var routingTargets = await RetrieveRoutingTargets(result);
            Assert.That(routingTargets, Has.All.Property("Endpoint").EqualTo("destination"));
        }

        [Test]
        public async Task WhenRoutingAssemblyWithNamespaceToEndpoint_ShouldOnlyConfigureMessagesWithinThatNamespace()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());

            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), nameof(MessageNamespaceA), "destination");

            var routingTable = routingSettings.Settings.Get<UnicastRoutingTable>();
            var result1 = await routingTable.GetDestinationsFor(new List<Type>
            {
                typeof(SomeMessageType)
            }, new ContextBag());
            Assert.That(result1.Count(), Is.EqualTo(1));

            var result2 = await routingTable.GetDestinationsFor(new List<Type>
            {
                typeof(OtherMessageType),
                typeof(MessageWithoutNamespace)
            }, new ContextBag());
            Assert.That(result2.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task WhenRoutingAssemblyWithNamespaceToEndpointAndSpecifyingNullNamespace_ShouldOnlyConfigureMessagesWithinEmptyNamespace()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());

            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), null, "destination");

            var routingTable = routingSettings.Settings.Get<UnicastRoutingTable>();
            var result1 = await routingTable.GetDestinationsFor(new List<Type>
            {
                typeof(MessageWithoutNamespace)
            }, new ContextBag());
            Assert.That(result1.Count(), Is.EqualTo(1));

            var result2 = await routingTable.GetDestinationsFor(new List<Type>
            {
                typeof(SomeMessageType),
                typeof(OtherMessageType)
            }, new ContextBag());
            Assert.That(result2.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task WhenRoutingAssemblyWithNamespaceToEndpointAndSpecifyingEmptyNamespace_ShouldOnlyConfigureMessagesWithinEmptyNamespace()
        {
            var routingSettings = new RoutingSettings(new SettingsHolder());

            routingSettings.RouteToEndpoint(Assembly.GetExecutingAssembly(), String.Empty, "destination");

            var routingTable = routingSettings.Settings.Get<UnicastRoutingTable>();
            var result1 = await routingTable.GetDestinationsFor(new List<Type>
            {
                typeof(MessageWithoutNamespace)
            }, new ContextBag());
            Assert.That(result1.Count(), Is.EqualTo(1));

            var result2 = await routingTable.GetDestinationsFor(new List<Type>
            {
                typeof(SomeMessageType),
                typeof(OtherMessageType)
            }, new ContextBag());
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