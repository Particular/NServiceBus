namespace NServiceBus.Core.Tests.Fakes;

using NUnit.Framework;
using Testing;

[TestFixture]
public class TestableMessageSessionTests
{
    [Test]
    public async System.Threading.Tasks.Task Subscribe_ShouldTrackSubscriptionsAsync()
    {
        var session = new TestableMessageSession();
        var options = new SubscribeOptions();

        await session.Subscribe(typeof(MyEvent), options);

        Assert.That(session.Subscriptions, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(session.Subscriptions[0].Options, Is.SameAs(options));
            Assert.That(session.Subscriptions[0].Message, Is.EqualTo(typeof(MyEvent)));
        });
    }

    [Test]
    public async System.Threading.Tasks.Task Unsubscribe_ShouldTrackUnsubscriptionsAsync()
    {
        var session = new TestableMessageSession();
        var options = new UnsubscribeOptions();

        await session.Unsubscribe(typeof(MyEvent), options);

        Assert.That(session.Unsubscription, Has.Length.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(session.Unsubscription[0].Options, Is.SameAs(options));
            Assert.That(session.Unsubscription[0].Message, Is.EqualTo(typeof(MyEvent)));
        });
    }

    class MyEvent
    {
    }
}