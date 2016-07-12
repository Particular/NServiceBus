namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using Transport;
    using NUnit.Framework;

    class SubscriptionRouterTests
    {
        [Test]
        public async Task Should_return_empty_list_for_events_with_no_routes()
        {
            var router = new SubscriptionRouter(new Publishers(), new EndpointInstances(), new TransportAddresses(address => null));
            Assert.IsEmpty(await router.GetAddressesForEventType(typeof(Message1)));
        }

        [Test]
        public async Task Should_return_correct_base_and_inherited_address_even_if_they_differ()
        {
            const string baseAddress = "baseAddress";
            const string inheritedAddress = "inheritedAddress";

            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);

            var publishers = new Publishers();
            publishers.Add(baseType, baseAddress);
            publishers.Add(baseType, inheritedAddress);
            publishers.Add(inheritedType, inheritedAddress);
            var endpointInstances = new EndpointInstances();
            endpointInstances.AddDynamic(e => Task.FromResult(EnumerableEx.Single(new EndpointInstance(e))));
            var physicalAddresses = new TransportAddresses(address => null);
            physicalAddresses.AddRule(i => i.EndpointInstance.Endpoint);
            var router = new SubscriptionRouter(publishers, endpointInstances, physicalAddresses);

            Assert.Contains(baseAddress, (await router.GetAddressesForEventType(baseType)).ToList());
            Assert.Contains(inheritedAddress, (await router.GetAddressesForEventType(baseType)).ToList());
            Assert.AreSame(inheritedAddress, (await router.GetAddressesForEventType(inheritedType)).Single());
        }

        [Test]
        public async Task Should_allow_multiple_addresses_per_type()
        {
            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);

            var publishers = new Publishers();
            publishers.Add(baseType, "addressA");
            publishers.Add(baseType, "addressB");
            publishers.Add(inheritedType, "addressB");
            var knownEndpoints = new EndpointInstances();
            knownEndpoints.AddDynamic(e => Task.FromResult(EnumerableEx.Single(new EndpointInstance(e, null, null))));
            var physicalAddresses = new TransportAddresses(address => null);
            physicalAddresses.AddRule(i => i.EndpointInstance.Endpoint);
            var router = new SubscriptionRouter(publishers, knownEndpoints, physicalAddresses);

            Assert.AreEqual(2, (await router.GetAddressesForEventType(baseType)).Count());
        }

        [Test]
        public void Should_not_generate_duplicate_addresses()
        {
        }

        public class Message1
        {
        }

        public class Message2
        {
        }

        public class BaseMessage : IEvent
        {
        }

        public class InheritedMessage : BaseMessage
        {
        }
    }
}