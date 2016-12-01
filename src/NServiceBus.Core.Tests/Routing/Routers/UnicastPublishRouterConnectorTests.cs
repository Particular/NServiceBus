namespace NServiceBus.Core.Tests.Routing.Routers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class UnicastPublishRouterConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_publish()
        {
            var router = new UnicastPublishRouterConnector(new FakeUnicastPubSub());
            var context = new TestableOutgoingPublishContext();

            await router.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Publish.ToString(), context.Headers[Headers.MessageIntent]);
        }

        class FakeUnicastPubSub : IUnicastPublish
        {
            public Task<List<UnicastRoutingStrategy>> GetRoutingStrategies(IOutgoingPublishContext context, Type eventType)
            {
                return Task.FromResult(new List<UnicastRoutingStrategy>()
                {
                    new UnicastRoutingStrategy("somewhere")
                });
            }
        }
    }
}