namespace NServiceBus.Core.Tests.Pipeline
{
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;
    using Outbox;

    [TestFixture]
    public class OutboxDeduplicationBehaviorTests
    {
        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();

            behavior = new OutboxDeduplicationBehavior
            {
                OutboxStorage = fakeOutbox
            };
        }

        void Invoke(ReceivePhysicalMessageContext context, bool shouldAbort = false)
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

        [Test]
        public void Should_mark_outbox_message_as_stored_when_successfully_processing_a_message()
        {
            var incomingTransportMessage = new TransportMessage();
            var context = new ReceivePhysicalMessageContext(null, incomingTransportMessage);

            Invoke(context);

            Assert.True(context.Get<bool>("Outbox_StartDispatching"), "Outbox message should be flaged as dispatching");
            Assert.True(fakeOutbox.StoredMessage.Dispatched);
        }

        [Test]
        public void Should_not_dispatch_already_dispatched_messages()
        {
            var incomingTransportMessage = new TransportMessage();

            fakeOutbox.ExistingMessage = new OutboxMessage(incomingTransportMessage.Id, true);
            fakeOutbox.ThrowOnDispatch();
            var context = new ReceivePhysicalMessageContext(null, incomingTransportMessage);

            Invoke(context);

            Assert.True(fakeOutbox.ExistingMessage.Dispatched);
            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public void Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            var incomingTransportMessage = new TransportMessage();

            fakeOutbox.ExistingMessage = new OutboxMessage(incomingTransportMessage.Id);

            var context = new ReceivePhysicalMessageContext(null, incomingTransportMessage);

            Invoke(context);

            Assert.True(fakeOutbox.ExistingMessage.Dispatched);
            Assert.Null(fakeOutbox.StoredMessage);
        }
    }
}