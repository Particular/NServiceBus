﻿namespace NServiceBus.Core.Tests.Routing.Routers
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class ReplyConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_reply()
        {
            var router = new ReplyConnector();
            var context = new TestableOutgoingReplyContext();
            context.Extensions.Set(new ReplyConnector.State { ExplicitDestination = "Fake" });

            await router.Invoke(context, ctx => Task.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntent.Reply.ToString(), context.Headers[Headers.MessageIntent]);
        }
    }
}