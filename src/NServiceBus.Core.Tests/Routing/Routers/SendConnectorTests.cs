namespace NServiceBus.Core.Tests.Routing;

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
public class SendConnectorTests
{
    [Test]
    public async Task Should_set_messageintent_to_send()
    {
        var physicalRouter = new FakeRouter { FixedDestination = new UnicastRoutingStrategy("destination endpoint") };

        var behavior = InitializeBehavior(physicalRouter);

        var context = CreateContext();

        await behavior.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers.Count, Is.EqualTo(1));
        Assert.That(context.Headers[Headers.MessageIntent], Is.EqualTo(MessageIntent.Send.ToString()));
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
            addressTag = (UnicastAddressTag)c.RoutingStrategies.Single().Apply([]);
            return Task.CompletedTask;
        });

        Assert.That(addressTag.Destination, Is.EqualTo("LogicalAddress"));
    }

    static TestableOutgoingSendContext CreateContext(SendOptions options = null, object message = null)
    {
        message ??= new MyMessage();

        var context = new TestableOutgoingSendContext
        {
            Message = new OutgoingLogicalMessage(message.GetType(), message),
            Extensions = options?.Context
        };
        return context;
    }


    static SendConnector InitializeBehavior(FakeRouter router = null)
    {
        var metadataRegistry = new MessageMetadataRegistry(new Conventions().IsMessageType, true);
        metadataRegistry.RegisterMessageTypes(
        [
            typeof(MyMessage),
            typeof(MessageWithoutRouting)
        ]);

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