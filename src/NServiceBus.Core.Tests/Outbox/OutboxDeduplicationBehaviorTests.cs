namespace NServiceBus.Core.Tests.Pipeline
{
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

            var context = new IncomingContext(null, incomingTransportMessage);

            Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public void Should_not_dispatch_the_message_if_handle_current_message_later_was_called()
        {
            var incomingTransportMessage = new TransportMessage();


            var context = new IncomingContext(null, incomingTransportMessage)
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

            behavior = new OutboxDeduplicationBehavior
            {
                OutboxStorage = fakeOutbox,
                TransactionSettings = TransactionSettings.Default
            };
        }

        void Invoke(IncomingContext context, bool shouldAbort = false)
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