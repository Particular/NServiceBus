namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;
    using Unicast.Messages;

    [TestFixture]
    public class SendConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_send()
        {
            var physicalRouter = new FakeRouter { FixedDestination = new UnicastRoutingStrategy("destination endpoint") };

            var behavior = InitializeBehavior(physicalRouter);

            var context = CreateContext();

            await behavior.Invoke(context, (ctx, ct) => Task.CompletedTask, CancellationToken.None);

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
            await behavior.Invoke(context, (ctx, ct) =>
            {
                addressTag = (UnicastAddressTag)ctx.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return Task.CompletedTask;
            }, CancellationToken.None);

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


        static SendConnector InitializeBehavior(FakeRouter router = null)
        {
            var metadataRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType);
            metadataRegistry.RegisterMessageTypesFoundIn(new List<Type>
            {
                typeof(MyMessage),
                typeof(MessageWithoutRouting)
            });

            return new SendConnector(router ?? new FakeRouter());
        }

        class FakeRouter : UnicastSendRouter
        {
            public FakeRouter() : base(false, null, null, null, null, null, null)
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