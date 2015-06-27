namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class DetermineRouteForReplyBehaviorTests
    {
        [Test]
        public void Should_default_to_reply_address_of_incoming_message_for_replies()
        {
            var behavior = new DetermineRouteForReplyBehavior();
            var options = new ReplyOptions();

            var context = new OutgoingReplyContext(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string> { { Headers.ReplyToAddress, "ReplyAddressOfIncomingMessage" } }, new MemoryStream()), null),new OutgoingLogicalMessage(new MyReply()), options);

            behavior.Invoke(context, () => { });

            var routingStrategy = (DirectToTargetDestination)context.Get<RoutingStrategy>();

            Assert.AreEqual("ReplyAddressOfIncomingMessage", routingStrategy.Destination);
        }

        [Test]
        public void Should_use_explicit_route_for_replies_if_present()
        {
            var behavior = new DetermineRouteForReplyBehavior();
            var options = new ReplyOptions();

            options.OverrideReplyToAddressOfIncomingMessage("CustomReplyToAddress");

            var context = new OutgoingReplyContext(new RootContext(null), new OutgoingLogicalMessage(new MyReply()), options);

            behavior.Invoke(context, () => { });

            var routingStrategy = (DirectToTargetDestination)context.Get<RoutingStrategy>();

            Assert.AreEqual("CustomReplyToAddress", routingStrategy.Destination);
        }

     
        class MyReply
        { }
    }
}