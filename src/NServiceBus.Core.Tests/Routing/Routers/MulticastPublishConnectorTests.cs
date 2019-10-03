namespace NServiceBus.Core.Tests.Routing.Routers
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class MulticastPublishConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_publish()
        {
            var router = new MulticastPublishConnector();
            var context = new TestableOutgoingPublishContext();

            await router.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Publish.ToString(), context.Headers[Headers.MessageIntent]);
        }
    }
}