namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Transport;

    [TestFixture]
    public class UnicastSendPhysicalRouterTests
    {
        [Test]
        public void Should_use_explicit_route_for_sends_if_present()
        {
            var router = new UnicastSend.PhysicalRouter("instanceSpecific", null);
            var options = new SendOptions();

            options.SetDestination("destination endpoint");

            var context = CreateContext(options);

            var result = router.Route(context);
            var addressTag = (UnicastAddressTag) result.Apply(new Dictionary<string, string>());

            Assert.AreEqual("destination endpoint", addressTag.Destination);
        }

        [Test]
        public void Should_route_to_local_instance_if_requested_so()
        {
            var router = new UnicastSend.PhysicalRouter("MyInstance", null);
            var options = new SendOptions();

            options.RouteToThisInstance();

            var context = CreateContext(options);

            var result = router.Route(context);
            var addressTag = (UnicastAddressTag)result.Apply(new Dictionary<string, string>());

            Assert.AreEqual("MyInstance", addressTag.Destination);
        }

        [Test]
        public void Should_return_no_route_for_this_endpoint_when_distributor_address_is_null()
        {
            var router = new UnicastSend.PhysicalRouter("MyInstance", null);
            var options = new SendOptions();

            options.RouteToThisEndpoint();

            var context = CreateContext(options);

            var result = router.Route(context);

            Assert.IsNull(result);
        }

        [Test]
        public void Should_return_route_for_this_endpoint_when_distributor_address_is_set()
        {
            var router = new UnicastSend.PhysicalRouter("MyInstance", "MyDistributor");
            var options = new SendOptions();
            options.RouteToThisEndpoint();

            var context = CreateContext(options);
            context.Extensions.Set(new IncomingMessage("messageId", new Dictionary<string, string>{ { LegacyDistributorHeaders.WorkerSessionId, string.Empty }}, new byte[0]));

            var result = router.Route(context);
            var addressTag = (UnicastAddressTag)result.Apply(new Dictionary<string, string>());

            Assert.AreEqual("MyDistributor", addressTag.Destination);
        }

        [Test]
        public void Should_return_no_route_for_specific_instance()
        {
            var router = new UnicastSend.PhysicalRouter("MyInstance", null);
            var options = new SendOptions();

            options.RouteToSpecificInstance("instanceId");

            var context = CreateContext(options);

            var result = router.Route(context);

            Assert.IsNull(result);
        }

        [Test]
        public void Should_throw_if_requested_to_route_to_local_instance_and_instance_has_no_specific_queue()
        {
            var router = new UnicastSend.PhysicalRouter(null, null);

            var options = new SendOptions();

            options.RouteToThisInstance();

            var context = CreateContext(options);

            var exception = Assert.Throws<InvalidOperationException>(() => router.Route(context));
            Assert.AreEqual(exception.Message, "Cannot route to a specific instance because an endpoint instance discriminator was not configured for the destination endpoint. It can be specified via EndpointConfiguration.MakeInstanceUniquelyAddressable(string discriminator).");
        }

        static IOutgoingSendContext CreateContext(SendOptions options, object message = null)
        {
            if (message == null)
            {
                message = new MyMessage();
            }

            var context = new TestableOutgoingSendContext
            {
                Message = new OutgoingLogicalMessage(message.GetType(), message),
                Extensions = options.Context
            };
            return context;
        }

        class MyMessage
        {
        }
    }
}