namespace NServiceBus.Core.Tests.Sagas
{
    using NUnit.Framework;
    using Unicast.Routing;

    [TestFixture]
    public class StaticMessageRouterTests
    {

        [Test]
        public void When_initialized_known_message_returns_undefined()
        {
            var router = new StaticMessageRouter(new[] { typeof(Message1) });
            Assert.AreEqual(Address.Undefined, router.GetDestinationFor(typeof(Message1)));
        }

        [Test]
        public void When_initialized_unknown_message_returns_undefined()
        {
            var router = new StaticMessageRouter(new[] { typeof(Message1) });
            Assert.AreEqual(Address.Undefined, router.GetDestinationFor(typeof(Message2)));
        }

        [Test]
        public void When_getting_route_correct_address_is_returned()
        {
            var router = new StaticMessageRouter(new[] { typeof(Message1) });
            var address = new Address("a","b");
            router.RegisterRoute(typeof(Message1), address);
            Assert.AreSame(address, router.GetDestinationFor(typeof(Message1)));
        }

        [Test]
        [Ignore("Will pass when we add support for this in v 4.2")]
        public void When_inherited_registered_after_base_correct_address_is_returned_for_both()
        {
            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);
            var router = new StaticMessageRouter(new[] { baseType, inheritedType });
            var baseAddress = new Address("baseAddress", "b");
            router.RegisterRoute(baseType, baseAddress);
            var inheritedAddress = new Address("inheritedAddress", "b");
            router.RegisterRoute(inheritedType, inheritedAddress);
            Assert.AreSame(baseAddress, router.GetDestinationFor(baseType));
            Assert.AreSame(inheritedAddress, router.GetDestinationFor(inheritedType));
        }

        [Test]
        public void When_inherited_base_after_registered_correct_address_is_returned_for_both()
        {
            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);
            var router = new StaticMessageRouter(new[] { baseType, inheritedType });
            var inheritedAddress = new Address("inheritedAddress", "b");
            router.RegisterRoute(inheritedType, inheritedAddress);
            var baseAddress = new Address("baseAddress", "b");
            router.RegisterRoute(baseType, baseAddress);
            Assert.AreSame(baseAddress, router.GetDestinationFor(baseType));
            Assert.AreSame(inheritedAddress, router.GetDestinationFor(inheritedType));
        }

        public class Message1
        {
            
        }
        public class Message2
        {
            
        }

        public class BaseMessage
        {
            
        }

        public class InheritedMessage : BaseMessage
        {
            
        }
    }
}