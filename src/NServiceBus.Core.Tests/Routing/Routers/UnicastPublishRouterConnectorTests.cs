namespace NServiceBus.Core.Tests.Routing.Routers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class UnicastPublishRouterConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_publish()
        {
            var router = new UnicastPublishRouterConnector(new FakePublishRouter(), new DistributionPolicy());
            var context = new TestableOutgoingPublishContext();

            await router.Invoke(context, ctx => TaskEx.CompletedTask);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Publish.ToString(), context.Headers[Headers.MessageIntent]);
        }

        class FakePublishRouter : IUnicastPublishRouter
        {
            public IEnumerable<UnicastRoutingStrategy> Route(Type messageType, IDistributionPolicy distributionPolicy, ContextBag contextBag)
            {
                var unicastRoutingStrategies = new List<UnicastRoutingStrategy>
                {
                    new UnicastRoutingStrategy("Fake")
                };
                return unicastRoutingStrategies;
            }
        }
    }
}