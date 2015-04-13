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


            subscriptionManager.Subscribe(typeof(TestEvent),"publish");

            sender.MessageAvailable.WaitOne();
            Assert.AreEqual(typeof(TestEvent).AssemblyQualifiedName,sender.MessageSent.Headers[Headers.SubscriptionMessageType] );
        }

        class FakeSender:ISendMessages
        {
            public FakeSender()
            {
                MessageAvailable = new AutoResetEvent(false);
            }


            public OutgoingMessage MessageSent { get; private set; }

            public TransportSendOptions SendOptions { get; private set; }

            public AutoResetEvent MessageAvailable { get; private set; }

            public void Send(OutgoingMessage message, TransportSendOptions sendOptions)
            {
                MessageSent = message;

                SendOptions = sendOptions;

                MessageAvailable.Set();
            }
        }

        class TestEvent
        {
            
        }
    }
}