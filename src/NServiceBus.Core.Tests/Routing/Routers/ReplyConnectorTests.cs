namespace NServiceBus.Core.Tests.Routing.Routers
{
    using System.Threading;
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

            await router.Invoke(context, (_, __) => Task.CompletedTask, default);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Reply.ToString(), context.Headers[Headers.MessageIntent]);
        }
    }
}