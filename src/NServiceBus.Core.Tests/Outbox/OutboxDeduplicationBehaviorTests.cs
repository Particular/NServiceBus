namespace NServiceBus.Core.Tests.Pipeline
{
    using System;
    using System.Transactions;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;
    using Outbox;
    using Unicast.Transport;

    [TestFixture]
    public class OutboxDeduplicationBehaviorTests
    {

        [Test]
        public void Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            var incomingTransportMessage = new TransportMessage();

            fakeOutbox.ExistingMessage = new OutboxMessage(incomingTransportMessage.Id);

            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(incomingTransportMessage, null));

            Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public void Should_not_dispatch_the_message_if_handle_current_message_later_was_called()
        {
            var incomingTransportMessage = new TransportMessage();

            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(incomingTransportMessage, null))
            {
                handleCurrentMessageLaterWasCalled = true
            };

            Invoke(context);

            Assert.False(fakeOutbox.WasDispatched);
        }

        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();
            var transactionSettings = new TransactionSettings(true, TimeSpan.FromSeconds(30), IsolationLevel.ReadCommitted, false, false);
            
            behavior = new OutboxDeduplicationBehavior(fakeOutbox, null, null,transactionSettings);
        }

        void Invoke(PhysicalMessageProcessingStageBehavior.Context context, bool shouldAbort = false)
        {
            behavior.Invoke(context, () =>
            {
                if (shouldAbort)
                {
                    Assert.Fail("Pipeline should be aborted");
                }
            });
        }

        FakeOutboxStorage fakeOutbox;
        OutboxDeduplicationBehavior behavior;
    }
}