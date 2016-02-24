﻿namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    public class UnicastSendRouterConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_send()
        {
            var behavior = InitializeBehavior();
            var options = new SendOptions();
            options.RouteTo(Destination.Address("destination endpoint"));
            var context = CreateContext(options);

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Send.ToString(), context.Headers[Headers.MessageIntent]);
        }

        [Test]
        public async Task Should_use_explicit_route_for_sends_if_present()
        {
            var behavior = InitializeBehavior();
            var options = new SendOptions();

            options.RouteTo(Destination.Address("destination endpoint"));

            var context = CreateContext(options);

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag) c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("destination endpoint", addressTag.Destination);
        }

        [Test]
        public async Task Should_route_to_local_endpoint_if_requested_so()
        {
            var behavior = InitializeBehavior(sharedQueue: "MyLocalAddress");
            var options = new SendOptions();

            options.RouteTo(Destination.ThisEndpoint);

            var context = CreateContext(options);

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag) c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("MyLocalAddress", addressTag.Destination);
        }

        [Test]
        public async Task Should_route_to_local_instance_if_requested_so()
        {
            var behavior = InitializeBehavior(sharedQueue: "MyLocalAddress", instanceSpecificQueue: "MyInstance");
            var options = new SendOptions();

            options.RouteTo(Destination.ThisInstance);

            var context = CreateContext(options);

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag) c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("MyInstance", addressTag.Destination);
        }

        [Test]
        public async Task Should_throw_if_requested_to_route_to_local_instance_and_instance_has_no_specific_queue()
        {
            var behavior = InitializeBehavior(sharedQueue: "MyLocalAddress", instanceSpecificQueue: null);

            try
            {
                var options = new SendOptions();

                options.RouteTo(Destination.ThisInstance);

                var context = CreateContext(options);
                await behavior.Invoke(context, c => TaskEx.CompletedTask);
                Assert.Fail("RouteToThisInstance");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        [Test]
        public async Task Should_route_using_the_mappings_if_no_destination_is_set()
        {
            var strategy = new FakeRoutingStrategy
            {
                FixedDestination = new[]
                {
                    new UnicastRoutingStrategy("MappedDestination")
                }
            };
            var behavior = InitializeBehavior(strategy: strategy);
            var options = new SendOptions();

            var context = CreateContext(options);

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag) c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("MappedDestination", addressTag.Destination);
        }

        [Test]
        public void Should_throw_if_no_route_can_be_found()
        {
            var strategy = new FakeRoutingStrategy
            {
                FixedDestination = new UnicastRoutingStrategy[]
                {
                }
            };

            var behavior = InitializeBehavior(strategy: strategy);
            var options = new SendOptions();

            var context = CreateContext(options, new MessageWithoutRouting());

            Assert.That(async () => await behavior.Invoke(context, _ => TaskEx.CompletedTask), Throws.InstanceOf<Exception>().And.Message.Contains("No destination specified"));
        }

        static IOutgoingSendContext CreateContext(SendOptions options, object message = null)
        {
            if (message == null)
            {
                message = new MyMessage();
            }

            var context = new OutgoingSendContext(new OutgoingLogicalMessage(message), options, new RootContext(null, null));
            return context;
        }


        static UnicastSendRouterConnector InitializeBehavior(
            string sharedQueue = null,
            string instanceSpecificQueue = null,
            FakeRoutingStrategy strategy = null)
        {
            var metadataRegistry = new MessageMetadataRegistry(new Conventions());
            metadataRegistry.RegisterMessageType(typeof(MyMessage));
            metadataRegistry.RegisterMessageType(typeof(MessageWithoutRouting));
            return new UnicastSendRouterConnector(sharedQueue, instanceSpecificQueue, strategy ?? new FakeRoutingStrategy(), new DistributionPolicy());
        }

        class FakeRoutingStrategy : IUnicastRouter
        {
            public IEnumerable<UnicastRoutingStrategy> FixedDestination { get; set; }

            public Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, DistributionStrategy distributionStrategy, ContextBag contextBag)
            {
                return Task.FromResult(FixedDestination);
            }
        }

        class MyMessage
        {
        }

        class MessageWithoutRouting
        {
        }
    }
}