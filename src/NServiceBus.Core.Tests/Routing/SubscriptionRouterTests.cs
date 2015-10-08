namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Transports;
    using NUnit.Framework;

    class SubscriptionRouterTests
    {
        [Test]
        public void Should_return_empty_list_for_events_with_no_routes()
        {
            var router = new SubscriptionRouter(new Publishers(), new EndpointInstances(), new TransportAddresses());
            Assert.IsEmpty(router.GetAddressesForEventType(typeof(Message1)));
        }
     
        [Test]
        public void Should_return_correct_base_and_inherited_address_even_if_they_differ()
        {
            const string baseAddress = "baseAddress";
            const string inheritedAddress = "inheritedAddress";

            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);
            var baseEndpoint = new EndpointName(baseAddress);
            var inheritedEndpoint = new EndpointName(inheritedAddress);

            var publishers = new Publishers();
            publishers.AddStatic(baseEndpoint, baseType );
            publishers.AddStatic(inheritedEndpoint, baseType);
            publishers.AddStatic(inheritedEndpoint, inheritedType );
            var knownEndpoints = new EndpointInstances();
            knownEndpoints.AddDynamic(e => new [] { new EndpointInstanceName(e, null, null) });
            var physicalAddresses = new TransportAddresses();
            physicalAddresses.AddRule(i => i.EndpointName.ToString());
            var router = new SubscriptionRouter(publishers, knownEndpoints, physicalAddresses);

            Assert.Contains(baseAddress, router.GetAddressesForEventType(baseType).ToList());
            Assert.Contains(inheritedAddress, router.GetAddressesForEventType(baseType).ToList());
            Assert.AreSame(inheritedAddress, router.GetAddressesForEventType(inheritedType).Single());
        }

        [Test]
        public void Should_allow_multiple_addresses_per_type()
        {
            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);

            var baseEndpoint = new EndpointName("addressA");
            var inheritedEndpoint = new EndpointName("addressB");

            var publishers = new Publishers();
            publishers.AddStatic(baseEndpoint, baseType);
            publishers.AddStatic(inheritedEndpoint, baseType);
            publishers.AddStatic(inheritedEndpoint, inheritedType);
            var knownEndpoints = new EndpointInstances();
            knownEndpoints.AddDynamic(e => new[] { new EndpointInstanceName(e, null, null) });
            var physicalAddresses = new TransportAddresses();
            physicalAddresses.AddRule(i => i.EndpointName.ToString());
            var router = new SubscriptionRouter(publishers, knownEndpoints, physicalAddresses);

            Assert.AreEqual(2, router.GetAddressesForEventType(baseType).Count());
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