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
            var physicalRouter = new FakeRouter { FixedDestination = new UnicastRoutingStrategy("destination endpoint") };

            var behavior = InitializeBehavior(physicalRouter);

            var context = CreateContext();

            await behavior.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Send.ToString(), context.Headers[Headers.MessageIntent]);
        }

        [Test]
        public async Task Should_use_router_to_route()
        {
            var logicalRouter = new FakeRouter { FixedDestination = new UnicastRoutingStrategy("LogicalAddress") };

            var behavior = InitializeBehavior(logicalRouter);

            var context = CreateContext();

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag)c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("LogicalAddress", addressTag.Destination);
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


        static UnicastSendRouterConnector InitializeBehavior(FakeRouter router = null)
        {
            var metadataRegistry = new MessageMetadataRegistry(new Conventions());
            metadataRegistry.RegisterMessageTypesFoundIn(new List<Type>
            {
                typeof(MyMessage),
                typeof(MessageWithoutRouting)
            });

            return new UnicastSendRouterConnector(router ?? new FakeRouter());
        }

        class FakeRouter : UnicastSendRouter
        {
            public FakeRouter() : base(null, null, null, null, null, null)
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