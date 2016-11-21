namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using System.Reflection;
    using MessageNamespaceA;
    using MessageNamespaceB;
    using NServiceBus.Config;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class MessageEndpointMappingTests
    {
        [Test]
        public void WhenRoutingMessageTypeToEndpoint_ShouldConfigureMessageTypeInRoutingTable()
        {
            var mappings = new MessageEndpointMappingCollection
            {
                new MessageEndpointMapping
                {
                    AssemblyName = Assembly.GetExecutingAssembly().FullName,
                    TypeFullName = typeof(SomeMessageType).FullName,
                    Endpoint = "destination"
                }
            };

            var routingTable = ApplyMappings(mappings);
            var route = routingTable.GetRoutesFor(typeof(SomeMessageType))?.Routes.FirstOrDefault();

            Assert.That(route, Is.Not.Null);
        }

        [Test]
        public void WhenRoutingAssemblyToEndpoint_ShouldConfigureAllContainedMessagesInRoutingTable()
        {
            var mappings = new MessageEndpointMappingCollection
            {
                new MessageEndpointMapping
                {
                    AssemblyName = Assembly.GetExecutingAssembly().FullName,
                    Endpoint = "destination"
                }
            };

            var routingTable = ApplyMappings(mappings);

            var someMessageRoute = routingTable.GetRoutesFor(typeof(SomeMessageType))?.Routes.FirstOrDefault();
            var otherMessageRoute = routingTable.GetRoutesFor(typeof(OtherMessageType))?.Routes.FirstOrDefault();
            var messageWithoutNamespaceRoute = routingTable.GetRoutesFor(typeof(MessageWithoutNamespace))?.Routes.FirstOrDefault();

            Assert.That(someMessageRoute, Is.Not.Null);
            Assert.That(otherMessageRoute, Is.Not.Null);
            Assert.That(messageWithoutNamespaceRoute, Is.Not.Null);
        }

        [Test]
        public void WhenRoutingAssemblyWithNamespaceToEndpoint_ShouldOnlyConfigureMessagesWithinThatNamespace()
        {
            var mappings = new MessageEndpointMappingCollection
            {
                new MessageEndpointMapping
                {
                    AssemblyName = Assembly.GetExecutingAssembly().FullName,
                    Namespace = nameof(MessageNamespaceA),
                    Endpoint = "destination"
                },
            };
            var routingTable = ApplyMappings(mappings);

            var someMessageRoute = routingTable.GetRoutesFor(typeof(SomeMessageType))?.Routes.FirstOrDefault();
            var otherMessageRoute = routingTable.GetRoutesFor(typeof(OtherMessageType))?.Routes.FirstOrDefault();
            var messageWithoutNamespaceRoute = routingTable.GetRoutesFor(typeof(MessageWithoutNamespace))?.Routes.FirstOrDefault();

            Assert.That(someMessageRoute, Is.Not.Null, "because SomeMessageType is in the given namespace");
            Assert.That(otherMessageRoute, Is.Null, "because OtherMessageType is not in the given namespace");
            Assert.That(messageWithoutNamespaceRoute, Is.Null, "because MessageWithoutNamespace is not in the given namespace");
        }

        [Test]
        public void WhenMultipleRoutesExist_ShouldUseMostSpecificOnes()
        {
            var mappings = new MessageEndpointMappingCollection
            {
                new MessageEndpointMapping
                {
                    AssemblyName = Assembly.GetExecutingAssembly().FullName,
                    TypeFullName = typeof(SomeMessageType).FullName,
                    Endpoint = "type_destination"
                },
                new MessageEndpointMapping
                {
                    AssemblyName = Assembly.GetExecutingAssembly().FullName,
                    Namespace = nameof(MessageNamespaceA),
                    Endpoint = "namespace_destination"
                },
                new MessageEndpointMapping
                {
                    AssemblyName = Assembly.GetExecutingAssembly().FullName,
                    Endpoint = "assembly_destination"
                }
            };
            var routingTable = ApplyMappings(mappings);

            var someMessageRoute = routingTable.GetRoutesFor(typeof(SomeMessageType))?.Routes.FirstOrDefault();
            var yetAnotherRoute = routingTable.GetRoutesFor(typeof(YetAnotherMessageType))?.Routes.FirstOrDefault();
            var otherMessageRoute = routingTable.GetRoutesFor(typeof(OtherMessageType))?.Routes.FirstOrDefault();

            Assert.AreEqual("type_destination", someMessageRoute.PhysicalAddress);
            Assert.AreEqual("namespace_destination", yetAnotherRoute.PhysicalAddress);
            Assert.AreEqual("assembly_destination", otherMessageRoute.PhysicalAddress);
        }

        static UnicastRoutingTable ApplyMappings(MessageEndpointMappingCollection mappings)
        {
            var routeTable = new UnicastRoutingTable();
            NServiceBus.Features.RoutingFeature.ApplyLegacyConfiguration(mappings, routeTable, x => x, new Conventions());
            return routeTable;
        }
    }
}
