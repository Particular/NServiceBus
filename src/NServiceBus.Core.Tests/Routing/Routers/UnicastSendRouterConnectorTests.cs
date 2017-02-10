namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Unicast.Messages;

    [TestFixture]
    public class UnicastSendRouterConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_send()
        {
            var physicalRouter = new FakePhysicalRouter { FixedDestination = new UnicastRoutingStrategy("destination endpoint") };

            var behavior = InitializeBehavior(physicalRouter);

            var context = CreateContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Send.ToString(), context.Headers[Headers.MessageIntent]);
        }

        [Test]
        public async Task Should_route_physical_when_physical_router_returned_route()
        {
            var physicalRouter = new FakePhysicalRouter { FixedDestination = new UnicastRoutingStrategy("PhysicalAddress") };

            var behavior = InitializeBehavior(physicalRouter);

            var context = CreateContext();

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag)c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("PhysicalAddress", addressTag.Destination);
        }

        [Test]
        public async Task Should_route_logical_when_logical_router_returned_route()
        {
            var logicalRouter = new FakeLogicalRouter() { FixedDestination = new UnicastRoutingStrategy("LogicalAddress") };

            var behavior = InitializeBehavior(logicalRouter: logicalRouter);

            var context = CreateContext();

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag)c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("LogicalAddress", addressTag.Destination);
        }


        [Test]
        public void Should_throw_if_no_route_can_be_found()
        {
            var logicalRouter = new FakeLogicalRouter
            {
                FixedDestination = null
            };

            var physicalRouter = new FakePhysicalRouter
            {
                FixedDestination = null
            };

            var behavior = InitializeBehavior(physicalRouter: physicalRouter, logicalRouter: logicalRouter);

            var context = CreateContext();

            Assert.That(async () => await behavior.Invoke(context, _ => TaskEx.CompletedTask), Throws.InstanceOf<Exception>().And.Message.Contains("No destination specified"));
        }

        static IOutgoingSendContext CreateContext(SendOptions options = null, object message = null)
        {
            if (message == null)
            {
                message = new MyMessage();
            }

            var context = new TestableOutgoingSendContext
            {
                Message = new OutgoingLogicalMessage(message.GetType(), message),
                Extensions = options?.Context
            };
            return context;
        }


        static UnicastSendRouterConnector InitializeBehavior(
            FakePhysicalRouter physicalRouter = null,
            FakeLogicalRouter logicalRouter = null)
        {
            var metadataRegistry = new MessageMetadataRegistry(new Conventions());
            metadataRegistry.RegisterMessageTypesFoundIn(new List<Type>
            {
                typeof(MyMessage),
                typeof(MessageWithoutRouting)
            });

            return new UnicastSendRouterConnector(physicalRouter ?? new FakePhysicalRouter(), logicalRouter ?? new FakeLogicalRouter());
        }

        class FakePhysicalRouter : UnicastSend.PhysicalRouter
        {
            public FakePhysicalRouter() : base(null, null)
            {
            }

            public UnicastRoutingStrategy FixedDestination { get; set; }

            public override UnicastRoutingStrategy Route(IOutgoingSendContext context)
            {
                return FixedDestination;
            }
        }

        class FakeLogicalRouter : UnicastSend.LogicalRouter
        {
            public FakeLogicalRouter() : base(null, null, null, null, null, null, null)
            {
            }

            public UnicastRoutingStrategy FixedDestination { get; set; }

            public override UnicastRoutingStrategy Route(IOutgoingSendContext context)
            {
                return FixedDestination;
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