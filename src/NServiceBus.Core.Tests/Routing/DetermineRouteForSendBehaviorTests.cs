namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class DetermineRouteForSendBehaviorTests
    {
        [Test]
        public void Should_use_explicit_route_for_sends_if_present()
        {
            var behavior = InitializeBehavior();
            var options = new SendOptions();

            options.SetDestination("destination endpoint");

            var context = CreateContext(options);

            behavior.Invoke(context, () => { });

            var routingStrategy = (DirectToTargetDestination)context.Get<RoutingStrategy>();

            Assert.AreEqual("destination endpoint", routingStrategy.Destination);
        }



        [Test]
        public void Should_route_to_local_endpoint_if_requested_so()
        {
            var behavior = InitializeBehavior("MyLocalAddress");
            var options = new SendOptions();

            options.RouteToLocalEndpointInstance();

            var context = CreateContext(options);


            behavior.Invoke(context, () => { });

            var routingStrategy = (DirectToTargetDestination)context.Get<RoutingStrategy>();


            Assert.AreEqual("MyLocalAddress", routingStrategy.Destination);
        }

        [Test]
        public void Should_route_using_the_mappings_if_no_destination_is_set()
        {
            var router = new FakeRouter();

            var behavior = InitializeBehavior(router: router);
            var options = new SendOptions();

            var context = CreateContext(options);


            behavior.Invoke(context, () => { });

            var routingStrategy = (DirectToTargetDestination)context.Get<RoutingStrategy>();

            Assert.AreEqual("MappedDestination", routingStrategy.Destination);
        }

        static OutgoingSendContext CreateContext(SendOptions options)
        {
            var context = new OutgoingSendContext(new RootContext(null), new OutgoingLogicalMessage(new MyMessage()), options);
            return context;
        }


        static DetermineRouteForSendBehavior InitializeBehavior(string localAddress = null, MessageRouter router = null)
        {
            return new DetermineRouteForSendBehavior(localAddress, router);
        }

        class MyMessage
        { }
        class FakeRouter : MessageRouter
        {
            public override bool TryGetRoute(Type messageType, out string destination)
            {
                if (messageType == typeof(MyMessage))
                {
                    destination = "MappedDestination";

                    return true;
                }

                destination = null;
                return false;
            }
        }
    }
}