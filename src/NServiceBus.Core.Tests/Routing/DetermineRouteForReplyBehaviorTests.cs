namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class DetermineRouteForReplyBehaviorTests
    {
        [Test]
        public async Task Should_default_to_reply_address_of_incoming_message_for_replies()
        {
            var behavior = new UnicastReplyRouterConnector();

            var context = CreateContext(new OutgoingLogicalMessage(typeof(MyReply), new MyReply()));

            context.Extensions.Set(new IncomingMessage(
                "id",
                new Dictionary<string, string>
                {
                    { Headers.ReplyToAddress, "ReplyAddressOfIncomingMessage" }
                },
                Stream.Null));

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

            var context = CreateContext(new OutgoingLogicalMessage(typeof(MyReply), new MyReply()));

            context.Extensions.Set(new IncomingMessage(
                "id",
                new Dictionary<string, string>(),
                Stream.Null));

            Assert.That(async () => await behavior.Invoke(context, _ => TaskEx.CompletedTask), Throws.InstanceOf<Exception>().And.Message.Contains(typeof(MyReply).FullName));
        }

        [Test]
        public async Task Should_use_explicit_route_for_replies_if_present()
        {
            var behavior = new UnicastReplyRouterConnector();
            var options = new ReplyOptions();

            options.SetDestination("CustomReplyToAddress");

            var context = CreateContext(new OutgoingLogicalMessage(typeof(MyReply), new MyReply()));
            context.Extensions = options.Context;

            UnicastAddressTag addressTag = null;
            await behavior.Invoke(context, c =>
            {
                addressTag = (UnicastAddressTag)c.RoutingStrategies.Single().Apply(new Dictionary<string, string>());
                return TaskEx.CompletedTask;
            });

            Assert.AreEqual("CustomReplyToAddress", addressTag.Destination);
        }

        TestableOutgoingReplyContext CreateContext(OutgoingLogicalMessage message)
        {
            var context = new TestableOutgoingReplyContext()
            {
                Message = message
            };
            
            return context;
        }

        class MyReply
        {
        }
    }
}