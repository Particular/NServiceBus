namespace NServiceBus.Core.Tests.Routing
{
    using System.Threading;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class SubscriptionManagerTests
    {
        [Test]
        public void Should_send_the_assemblyQualified_name_as_subscription_type()
        {
            var sender = new FakeDispatcher();

            var subscriptionManager = new SubscriptionManager("subscriber", sender);


            subscriptionManager.Subscribe(typeof(TestEvent),"publish");

            sender.MessageAvailable.WaitOne();
            Assert.AreEqual(typeof(TestEvent).AssemblyQualifiedName,sender.MessageSent.Headers[Headers.SubscriptionMessageType] );
        }

        class FakeDispatcher:IDispatchMessages
        {
            public FakeDispatcher()
            {
                MessageAvailable = new AutoResetEvent(false);
            }


            public OutgoingMessage MessageSent { get; private set; }

            public DispatchOptions SendOptions { get; private set; }

            public AutoResetEvent MessageAvailable { get; private set; }

            public void Dispatch(OutgoingMessage message, DispatchOptions dispatchOptions)
            {
                MessageSent = message;

                SendOptions = dispatchOptions;

                MessageAvailable.Set();
            }
        }

        class TestEvent
        {
            
        }
    }
}