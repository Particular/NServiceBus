namespace NServiceBus.Core.Tests.Pipeline
{
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;
    using Outbox;

    [TestFixture]
    public class OutboxReceiveBehaviorTests
    {
        [Test]
        public void Should_mark_outbox_message_as_stored_when_successfully_processing_a_message()
        {
            var incomingTransportMessage = new TransportMessage();
            var context = new PhysicalMessageContext(null, incomingTransportMessage);

            Invoke(context);

            Assert.True(context.Get<OutboxMessage>().IsDispatching,"Outbox message should be flaged as dispatching");
            Assert.True(fakeOutbox.StoredMessage.Dispatched);
        }

        [Test]
        public void Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            var incomingTransportMessage = new TransportMessage();

            fakeOutbox.ExistingMessage = new OutboxMessage { Id = incomingTransportMessage.Id };

            var context = new PhysicalMessageContext(null, incomingTransportMessage);

            Invoke(context);

            Assert.True(fakeOutbox.ExistingMessage.Dispatched);
            Assert.Null(fakeOutbox.StoredMessage);
        }

        void Invoke(PhysicalMessageContext context, bool shouldAbort = false)
        {
            behavior.Invoke(context, () =>
            {
                if (shouldAbort)
                {
                    Assert.Fail("Pipeline should be aborted");
                }
            });
        }

     
        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();

            behavior = new OutboxReceiveBehavior
            {
                OutboxStorage = fakeOutbox
            };

        }

        FakeOutboxStorage fakeOutbox;
        OutboxReceiveBehavior behavior;

    }
}