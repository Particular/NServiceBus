namespace NServiceBus.Core.Tests.Routing.Routers;

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
        replyOptions.SetDestination("Fake");

        var context = new TestableOutgoingReplyContext()
        {
            Extensions = replyOptions.Context
        };

        await router.Invoke(context, ctx => Task.CompletedTask);

        Assert.That(context.Headers.Count, Is.EqualTo(1));
        Assert.That(context.Headers[Headers.MessageIntent], Is.EqualTo(MessageIntent.Reply.ToString()));
    }
}