namespace NServiceBus.Core.Tests.Routing.Routers
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
            var replyOptions = new ReplyOptions();
            replyOptions.RouteReplyTo("Fake");


            var context = new TestableOutgoingReplyContext();
            context.Extensions.Merge(replyOptions.Context);

            await router.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Reply.ToString(), context.Headers[Headers.MessageIntent]);
        }
    }
}