namespace NServiceBus.Core.Tests.Routing.Routers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class UnicastPublishConnectorTests
    {
        [Test]
        public async Task Should_set_messageintent_to_publish()
        {
            var router = new UnicastPublishConnector(new FakePublishRouter(), new DistributionPolicy());
            var context = new TestableOutgoingPublishContext();

            await router.Invoke(context, (_, __) => Task.CompletedTask, default);

            Assert.AreEqual(1, context.Headers.Count);
            Assert.AreEqual(MessageIntentEnum.Publish.ToString(), context.Headers[Headers.MessageIntent]);
        }

        class FakePublishRouter : IUnicastPublishRouter
        {
            public Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, IOutgoingPublishContext publishContext)
            {
                IEnumerable<UnicastRoutingStrategy> unicastRoutingStrategies = new List<UnicastRoutingStrategy>
                {
                    new UnicastRoutingStrategy("Fake")
                };
                return Task.FromResult(unicastRoutingStrategies);
            }
        }
    }
}