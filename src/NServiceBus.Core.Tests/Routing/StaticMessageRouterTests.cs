namespace NServiceBus.Core.Tests.Sagas
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Unicast.Routing;

    [TestFixture]
    public class StaticMessageRouterTests
    {
        [Test]
        public void When_initialized_known_message_returns_empty()
        {
            var router = new StaticMessageRouter(new[]
            {
                typeof(Message1)
            });
            Assert.IsEmpty(router.GetDestinationFor(typeof(Message1)));
        }

        [Test]
        public void When_route_with_undefined_address_is_registered_exception_is_thrown()
        {
            var router = new StaticMessageRouter(new Type[0]);

            Assert.Throws<InvalidOperationException>(() => router.RegisterMessageRoute(typeof(Message1), null));
        }

        [Test]
        public void Test_that_expose_the_issue_with_the_current_codebase_assuming_that_routes_can_be_updated()
        {
            var router = new StaticMessageRouter(new Type[0]);

            var overrideAddress = "override";

            router.RegisterMessageRoute(typeof(Message1), "first");
            router.RegisterMessageRoute(typeof(Message1), overrideAddress);

            Assert.AreEqual(overrideAddress, router.GetDestinationFor(typeof(Message1)).Single());
        }

        [Test]
        public void When_initialized_unknown_message_returns_empty()
        {
            var router = new StaticMessageRouter(new[]
            {
                typeof(Message1)
            });
            Assert.IsEmpty(router.GetDestinationFor(typeof(Message2)));
        }

        [Test]
        public void When_getting_route_correct_address_is_returned()
        {
            var router = new StaticMessageRouter(new[]
            {
                typeof(Message1)
            });
            var address = "a@b";
            router.RegisterMessageRoute(typeof(Message1), address);
            Assert.AreSame(address, router.GetDestinationFor(typeof(Message1)).Single());
        }

        [Test]
        [Ignore("Will pass when we add support for this in v 4.2")]
        public void When_inherited_registered_after_base_correct_address_is_returned_for_both()
        {
            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);
            var router = new StaticMessageRouter(new[]
            {
                baseType,
                inheritedType
            });
            var baseAddress = "baseAddress@b";
            router.RegisterMessageRoute(baseType, baseAddress);
            var inheritedAddress = "inheritedAddress@b";
            router.RegisterMessageRoute(inheritedType, inheritedAddress);
            Assert.Contains(baseAddress, router.GetDestinationFor(baseType));
            Assert.Contains(inheritedAddress, router.GetDestinationFor(baseType));
            Assert.AreSame(inheritedAddress, router.GetDestinationFor(inheritedType).Single());
        }

        [Test]
        public void When_inherited_base_after_registered_correct_address_is_returned_for_both()
        {
            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);
            var router = new StaticMessageRouter(new[]
            {
                baseType,
                inheritedType
            });
            var inheritedAddress = "inheritedAddress@b";
            router.RegisterEventRoute(inheritedType, inheritedAddress);
            var baseAddress = "baseAddress@b";
            router.RegisterEventRoute(baseType, baseAddress);
            Assert.Contains(baseAddress, router.GetDestinationFor(baseType));
            Assert.Contains(inheritedAddress, router.GetDestinationFor(baseType));
            Assert.AreSame(inheritedAddress, router.GetDestinationFor(inheritedType).Single());
        }

        [Test]
        public void When_registered_registering_multiple_addresses_for_same_type_same_number_of_addresses_are_returned()
        {
            var baseType = typeof(BaseMessage);
            var router = new StaticMessageRouter(Enumerable.Empty<Type>());
            var addressA = "BaseMessage@A";
            router.RegisterEventRoute(baseType, addressA);
            var addressB = "BaseMessage@b";
            router.RegisterEventRoute(baseType, addressB);

            Assert.AreEqual(2, router.GetDestinationFor(baseType).Count);
        }

        [Test]
        public void When_registered_registering_multiple_addresses_for_same_type_and_using_plainmessages_last_one_wins()
        {
            var baseType = typeof(BaseMessage);
            var router = new StaticMessageRouter(Enumerable.Empty<Type>());
            var addressA = "BaseMessage@A";
            router.RegisterMessageRoute(baseType, addressA);
            var addressB = "BaseMessage@b";
            router.RegisterMessageRoute(baseType, addressB);

            Assert.AreEqual(1, router.GetDestinationFor(baseType).Count);
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