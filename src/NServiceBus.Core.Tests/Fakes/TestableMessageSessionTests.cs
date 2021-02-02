namespace NServiceBus.Core.Tests.Fakes
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class TestableMessageSessionTests
    {
        [Test]
        public async Task Subscribe_ShouldTrackSubscriptions()
        {
            var session = new TestableMessageSession();
            var options = new SubscribeOptions();

            await session.Subscribe(typeof(MyEvent), options);

            Assert.AreEqual(1, session.Subscriptions.Length);
            Assert.AreSame(options, session.Subscriptions[0].Options);
            Assert.AreEqual(typeof(MyEvent), session.Subscriptions[0].Message);
        }

        [Test]
        public async Task Unsubscribe_ShouldTrackUnsubscriptions()
        {
            var session = new TestableMessageSession();
            var options = new UnsubscribeOptions();

            await session.Unsubscribe(typeof(MyEvent), options);

            Assert.AreEqual(1, session.Unsubscription.Length);
            Assert.AreSame(options, session.Unsubscription[0].Options);
            Assert.AreEqual(typeof(MyEvent), session.Unsubscription[0].Message);
        }

        class MyEvent
        {
        }
    }
}