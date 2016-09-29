namespace NServiceBus.Core.Tests.Routing
{
    using System.Reflection;
    using MessageNamespaceA;
    using MessageNamespaceB;
    using NServiceBus.Config;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
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
            var route = routingTable.GetRouteFor(typeof(SomeMessageType));

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

            var someMessageRoute = routingTable.GetRouteFor(typeof(SomeMessageType));
            var otherMessageRoute = routingTable.GetRouteFor(typeof(OtherMessageType));
            var messageWithoutNamespaceRoute = routingTable.GetRouteFor(typeof(MessageWithoutNamespace));

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

            var someMessageRoute = routingTable.GetRouteFor(typeof(SomeMessageType));
            var yetAnotherRoute = routingTable.GetRouteFor(typeof(YetAnotherMessageType));
            var otherMessageRoute = routingTable.GetRouteFor(typeof(OtherMessageType));

            Assert.AreEqual("type_destination", someMessageRoute.PhysicalAddress);
            Assert.AreEqual("namespace_destination", yetAnotherRoute.PhysicalAddress);
            Assert.AreEqual("assembly_destination", otherMessageRoute.PhysicalAddress);
        }

        static UnicastRoutingTable ApplyMappings(MessageEndpointMappingCollection mappings)
        {
            var routeTable = new UnicastRoutingTable();
            mappings.Apply(new Publishers(), routeTable, x => x, new Conventions());
            return routeTable;
        }
    }
}
