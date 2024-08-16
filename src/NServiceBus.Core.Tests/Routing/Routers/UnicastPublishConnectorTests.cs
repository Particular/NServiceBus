namespace NServiceBus.Core.Tests.Routing.Routers;

using System;
using System.Collections.Generic;
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

        await router.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers.Count, Is.EqualTo(1));
        Assert.That(context.Headers[Headers.MessageIntent], Is.EqualTo(MessageIntent.Publish.ToString()));
    }

    class FakePublishRouter : IUnicastPublishRouter
    {
        public Task<IEnumerable<UnicastRoutingStrategy>> Route(Type messageType, IDistributionPolicy distributionPolicy, IOutgoingPublishContext publishContext)
        {
            IEnumerable<UnicastRoutingStrategy> unicastRoutingStrategies =
            [
                new UnicastRoutingStrategy("Fake")
            ];
            return Task.FromResult(unicastRoutingStrategies);
        }
    }
}