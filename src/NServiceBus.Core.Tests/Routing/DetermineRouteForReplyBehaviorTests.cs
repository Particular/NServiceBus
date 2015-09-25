namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using Transports;
    using NUnit.Framework;

    [TestFixture]
    public class DetermineRouteForReplyBehaviorTests
    {
        [Test]
        public async Task Should_default_to_reply_address_of_incoming_message_for_replies()
        {
            var behavior = new DetermineRouteForReplyBehavior();
            var options = new ReplyOptions();

            var context = new OutgoingReplyContext(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>
            {
                {Headers.ReplyToAddress, "ReplyAddressOfIncomingMessage"}
            }, new MemoryStream()), new RootContext(null)), new OutgoingLogicalMessage(new MyReply()), options);

            await behavior.Invoke(context, () => Task.FromResult(0));

            var routingStrategy = (DirectToTargetDestination) context.Get<RoutingStrategy>();

            Assert.AreEqual("ReplyAddressOfIncomingMessage", routingStrategy.Destination);
        }

        [Test]
        public void Should_throw_if_incoming_message_has_no_reply_to_address()
        {
            var behavior = new DetermineRouteForReplyBehavior();
            var options = new ReplyOptions();

            var context = new OutgoingReplyContext(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), new RootContext(null)), new OutgoingLogicalMessage(new MyReply()), options);

            var ex = Assert.Throws<Exception>(async () => await behavior.Invoke(context, () => Task.FromResult(0)));

            Assert.True(ex.Message.Contains(typeof(MyReply).FullName));
        }

        [Test]
        public async Task Should_use_explicit_route_for_replies_if_present()
        {
            var behavior = new DetermineRouteForReplyBehavior();
            var options = new ReplyOptions();

            options.OverrideReplyToAddressOfIncomingMessage("CustomReplyToAddress");

            var context = new OutgoingReplyContext(new RootContext(null), new OutgoingLogicalMessage(new MyReply()), options);

            await behavior.Invoke(context, () => Task.FromResult(0));

            var routingStrategy = (DirectToTargetDestination) context.Get<RoutingStrategy>();

            Assert.AreEqual("CustomReplyToAddress", routingStrategy.Destination);
        }

        class MyReply
        {
        }
    }
}