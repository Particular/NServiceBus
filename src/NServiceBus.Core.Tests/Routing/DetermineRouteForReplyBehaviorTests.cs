namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class DetermineRouteForReplyBehaviorTests
    {
        [Test]
        public async Task Should_default_to_reply_address_of_incoming_message_for_replies()
        {
            var behavior = new UnicastReplyRouterConnector();
            var options = new ReplyOptions();

            var context = new OutgoingReplyContext(
                new OutgoingLogicalMessage(new MyReply()),
                options,
                new TransportReceiveContext(
                    new IncomingMessage(
                        "id",
                        new Dictionary<string, string>
                        {
                            {Headers.ReplyToAddress, "ReplyAddressOfIncomingMessage"}
                        },
                        new MemoryStream()), null, new CancellationTokenSource(),
                    new RootContext(null, null)));

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag)c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("ReplyAddressOfIncomingMessage", addressTag.Destination);
        }

        [Test]
        public void Should_throw_if_incoming_message_has_no_reply_to_address()
        {
            var behavior = new UnicastReplyRouterConnector();
            var options = new ReplyOptions();

            var context = new OutgoingReplyContext(
                new OutgoingLogicalMessage(new MyReply()),
                options,
                new TransportReceiveContext(
                    new IncomingMessage(
                        "id",
                        new Dictionary<string, string>(),
                        new MemoryStream()), null, new CancellationTokenSource(),
                    new RootContext(null, null)));

            Assert.That(async () => await behavior.Invoke(context, _ => TaskEx.CompletedTask), Throws.InstanceOf<Exception>().And.Message.Contains(typeof(MyReply).FullName));
        }

        [Test]
        public async Task Should_use_explicit_route_for_replies_if_present()
        {
            var behavior = new UnicastReplyRouterConnector();
            var options = new ReplyOptions();

            options.RouteTo(Destination.Address("CustomReplyToAddress"));

            var context = new OutgoingReplyContext(
                new OutgoingLogicalMessage(new MyReply()),
                options,
                new RootContext(null, null));

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag)c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("CustomReplyToAddress", addressTag.Destination);
        }

        class MyReply
        {
        }
    }
}