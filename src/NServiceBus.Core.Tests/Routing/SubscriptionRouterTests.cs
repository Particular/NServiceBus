namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Linq;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;

    class SubscriptionRouterTests
    {
        [Test]
        public void Should_return_empty_list_for_events_with_no_routes()
        {
            var router = new SubscriptionRouter( new StaticRoutes(), new[]
            {
                typeof(Message1)
            });

            Assert.IsEmpty(router.GetAddressesForEventType(typeof(Message1)));
        }

        [Test]
        public void Should_prevent_invalid_addresses_to_be_used()
        {
            var routes = new StaticRoutes();

            Assert.Throws<ArgumentNullException>(() => routes.Register(typeof(Message1), null));
            Assert.Throws<ArgumentNullException>(() => routes.Register(typeof(Message1), ""));
        }

     
        [Test]
        public void Should_return_correct_base_and_inherited_address_even_if_they_differ()
        {
            var baseType = typeof(BaseMessage);
            var inheritedType = typeof(InheritedMessage);
            var inheritedAddress = "inheritedAddress";
            var baseAddress = "baseAddress";
   
            var staticRoutes = new StaticRoutes();

            staticRoutes.Register(inheritedType,inheritedAddress);
            staticRoutes.Register(baseType, baseAddress);

            var router = new SubscriptionRouter(staticRoutes, new[]
            {
                baseType,
                inheritedType
            });

            Assert.Contains(baseAddress, router.GetAddressesForEventType(baseType).ToList());
            Assert.Contains(inheritedAddress, router.GetAddressesForEventType(baseType).ToList());
            Assert.AreSame(inheritedAddress, router.GetAddressesForEventType(inheritedType).Single());
        }

        [Test]
        public void Should_allow_multiple_addresses_per_type()
        {
            var baseType = typeof(BaseMessage);
         
            var staticRoutes = new StaticRoutes();

            staticRoutes.Register(baseType, "addressA");
            staticRoutes.Register(typeof(InheritedMessage), "addressB");

            var router = new SubscriptionRouter(staticRoutes, new[]
            {
                baseType
            });

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