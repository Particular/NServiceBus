namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using NServiceBus.Routing;
    using Transports;
    using NUnit.Framework;

    [TestFixture]
    public class DetermineRouteForReplyBehaviorTests
    {
        [Test]
        public async Task Should_default_to_reply_address_of_incoming_message_for_replies()
        {
            var behavior = new UnicastReplyRouterConnector();
            var options = new ReplyOptions();

            var context = new OutgoingReplyContextImpl(
                new OutgoingLogicalMessage(new MyReply()),
                options,
                new TransportReceiveContextImpl(
                    new IncomingMessage(
                        "id",
                        new Dictionary<string, string>
                        {
                            {Headers.ReplyToAddress, "ReplyAddressOfIncomingMessage"}
                        },
                        new MemoryStream()), null,
                    new RootContext(null)));

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag) c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return Task.FromResult(0);
            });

            Assert.AreEqual("ReplyAddressOfIncomingMessage", addressTag.Destination);
        }

        [Test]
        public void Should_throw_if_incoming_message_has_no_reply_to_address()
        {
            var behavior = new UnicastReplyRouterConnector();
            var options = new ReplyOptions();

            var context = new OutgoingReplyContextImpl(
                new OutgoingLogicalMessage(new MyReply()),
                options,
                new TransportReceiveContextImpl(
                    new IncomingMessage(
                        "id",
                        new Dictionary<string, string>(),
                        new MemoryStream()), null,
                    new RootContext(null)));

            var ex = Assert.Throws<Exception>(async () => await behavior.Invoke(context, _ => Task.FromResult(0)));

            Assert.True(ex.Message.Contains(typeof(MyReply).FullName));
        }

        [Test]
        public async Task Should_use_explicit_route_for_replies_if_present()
        {
            var behavior = new UnicastReplyRouterConnector();
            var options = new ReplyOptions();

            options.OverrideReplyToAddressOfIncomingMessage("CustomReplyToAddress");

            var context = new OutgoingReplyContextImpl(
                new OutgoingLogicalMessage(new MyReply()),
                options,
                new RootContext(null));

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag) c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return Task.FromResult(0);
            });

            Assert.AreEqual("CustomReplyToAddress", addressTag.Destination);
        }

        class MyReply
        {
        }
    }
}