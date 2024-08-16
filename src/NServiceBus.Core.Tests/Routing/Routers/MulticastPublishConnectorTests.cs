namespace NServiceBus.Core.Tests.Routing.Routers;

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

        await router.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers.Count, Is.EqualTo(1));
        Assert.That(context.Headers[Headers.MessageIntent], Is.EqualTo(MessageIntent.Publish.ToString()));
    }
}