namespace NServiceBus.Unicast.Tests
{
    using System.Threading;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture]
    public class SubscriptionManagerTests
    {
        [Test]
        public void Should_send_the_assemblyQualified_name_as_subscription_type()
        {
            var sender = new FakeSender();

            var subscriptionManager = new SubscriptionManager("subscriber", sender);


            subscriptionManager.Subscribe(typeof(TestEvent),Address.Parse("publish") );

            sender.MessageAvailable.WaitOne();
            Assert.AreEqual(typeof(TestEvent).AssemblyQualifiedName,sender.MessageSent.Headers[Headers.SubscriptionMessageType] );
        }

        class FakeSender:ISendMessages
        {
            public FakeSender()
            {
                MessageAvailable = new AutoResetEvent(false);
            }
            public TransportMessage MessageSent { get; private set; }

            public AutoResetEvent MessageAvailable { get; private set; }

            public void Send(TransportMessage message, SendOptions sendOptions)
            {
                MessageSent = message;

                MessageAvailable.Set();
            }
        }

        class TestEvent
        {
            
        }
    }
}