namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Extensibility;
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
        public async Task WhenRoutingMessageTypeToEndpoint_ShouldConfigureMessageTypeInRoutingTable()
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
            var route = await routingTable.GetRouteFor(typeof(SomeMessageType), new ContextBag());

            Assert.That(route, Is.Not.Null);
        }

        [Test]
        public async Task WhenRoutingAssemblyToEndpoint_ShouldConfigureAllContainedMessagesInRoutingTable()
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

            var someMessageRoute = await routingTable.GetRouteFor(typeof(SomeMessageType), new ContextBag());
            var otherMessageRoute = await routingTable.GetRouteFor(typeof(OtherMessageType), new ContextBag());
            var messageWithoutNamespaceRoute = await routingTable.GetRouteFor(typeof(MessageWithoutNamespace), new ContextBag());

            Assert.That(someMessageRoute, Is.Not.Null, "because SomeMessageType is in the given namespace");
            Assert.That(otherMessageRoute, Is.Null, "because OtherMessageType is not in the given namespace");
            Assert.That(messageWithoutNamespaceRoute, Is.Null, "because MessageWithoutNamespace is not in the given namespace");
        }

        [Test]
        public async Task WhenMultipleRoutesExist_ShouldUseMostSpecificOnes()
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

            var someMessageRoute = await routingTable.GetRouteFor(typeof(SomeMessageType), new ContextBag());
            var yetAnotherRoute = await routingTable.GetRouteFor(typeof(YetAnotherMessageType), new ContextBag());
            var otherMessageRoute = await routingTable.GetRouteFor(typeof(OtherMessageType), new ContextBag());

            var someMessageDestion = await GetDestination(someMessageRoute);
            var otherMessageDestination = await GetDestination(otherMessageRoute);
            var yetAnotherDestination = await GetDestination(yetAnotherRoute);

            Assert.AreEqual("type_destination", someMessageDestion);
            Assert.AreEqual("namespace_destination", yetAnotherDestination);
            Assert.AreEqual("assembly_destination", otherMessageDestination);
        }

        static UnicastRoutingTable ApplyMappings(MessageEndpointMappingCollection mappings)
        {
            var routeTable = new UnicastRoutingTable();
            mappings.ImportMessageEndpointMappings(new Publishers(), routeTable, x => x);
            return routeTable;
        }

        static async Task<string> GetDestination(IUnicastRoute result)
        {
            var targets = await result.Resolve(e => Task.FromResult<IEnumerable<EndpointInstance>>(new[]
            {
                new EndpointInstance(e)
            }));
            return targets.Single().TransportAddress;
        }
    }
}
