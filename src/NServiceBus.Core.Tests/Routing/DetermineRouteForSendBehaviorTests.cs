namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class DetermineRouteForSendBehaviorTests
    {
        [Test]
        public async Task Should_use_explicit_route_for_sends_if_present()
        {
            var behavior = InitializeBehavior();
            var options = new SendOptions();

            options.SetDestination("destination endpoint");

            var context = CreateContext(options);

            await behavior.Invoke(context, () => Task.FromResult(0));

            var routingStrategy = (DirectAddressLabel)context.Get<AddressLabel[]>().First();

            Assert.AreEqual("destination endpoint", routingStrategy.Destination);
        }

        [Test]
        public async Task Should_route_to_local_endpoint_if_requested_so()
        {
            var behavior = InitializeBehavior("MyLocalAddress");
            var options = new SendOptions();

            options.RouteToLocalEndpointInstance();

            var context = CreateContext(options);

            await behavior.Invoke(context, () => Task.FromResult(0));

            var routingStrategy = (DirectAddressLabel)context.Get<AddressLabel[]>().First();

            Assert.AreEqual("MyLocalAddress", routingStrategy.Destination);
        }

        [Test]
        public async Task Should_route_using_the_mappings_if_no_destination_is_set()
        {
            var strategy = new FakeRoutingStrategy()
            {
                FixedDestination = new AddressLabel[] { new DirectAddressLabel("MappedDestination")}
            };
            var behavior = InitializeBehavior(strategy:strategy);
            var options = new SendOptions();

            var context = CreateContext(options);

            await behavior.Invoke(context, () => Task.FromResult(0));

            var routingStrategy = (DirectAddressLabel)context.Get<AddressLabel[]>().First();

            Assert.AreEqual("MappedDestination", routingStrategy.Destination);
        }

        [Test]
        public void Should_throw_if_no_route_can_be_found()
        {
            var strategy = new FakeRoutingStrategy()
            {
                FixedDestination = new AddressLabel[] {}
            };

            var behavior = InitializeBehavior(strategy: strategy);
            var options = new SendOptions();

            var context = CreateContext(options, new MessageWithoutRouting());

            var ex = Assert.Throws<Exception>(async() => await behavior.Invoke(context, () => Task.FromResult(0)));

            Assert.True(ex.Message.Contains("No destination specified"));
        }

        static OutgoingSendContext CreateContext(SendOptions options, object message = null)
        {
            if (message == null)
            {
                message = new MyMessage();
            }

            var context = new OutgoingSendContext(new OutgoingLogicalMessage(message), options, new RootContext(null));
            return context;
        }


        static DirectSendRouterBehavior InitializeBehavior(string localAddress = null,
            FakeRoutingStrategy strategy = null)
        {
            var metadataRegistry = new MessageMetadataRegistry(new Conventions());
            metadataRegistry.RegisterMessageType(typeof(MyMessage));
            metadataRegistry.RegisterMessageType(typeof(MessageWithoutRouting));
            return new DirectSendRouterBehavior(localAddress, strategy ?? new FakeRoutingStrategy(), new DistributionPolicy());
        }

        class FakeRoutingStrategy : IDirectRoutingStrategy
        {
            public IEnumerable<AddressLabel> FixedDestination { get; set; } 

            public IEnumerable<AddressLabel> Route(Type messageType, DistributionStrategy distributionStrategy, ContextBag contextBag)
            {
                return FixedDestination;
            }
        }

        class MyMessage { }

        class MessageWithoutRouting { }
    }
}