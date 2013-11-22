namespace NServiceBus.Core.Tests.Pipeline
{
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    [TestFixture]
    public class OutboxReceiveBehaviourTests
    {
        FakeOutboxStorage fakeOutbox;
        OutboxReceiveBehaviour behavior;

        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();
         
            behavior = new OutboxReceiveBehaviour
            {
                OutboxStorage = fakeOutbox
            };

        }

        void Invoke(TransportMessage message,bool shouldAbort = false)
        {
            var context = new PhysicalMessageContext(null, message);
            behavior.Invoke(context, () =>
            {
                if (shouldAbort)
                {
                    Assert.Fail("Pipeline should be aborted");
                }
            });
        }


        [Test]
        public void Should_mark_outbox_message_as_stored_when_successfully_processing_a_message()
        {
            var incomingTransportMessage = new TransportMessage();

            Invoke(incomingTransportMessage);

            Assert.True(fakeOutbox.StoredMessage.Dispatched);
        }

        [Test]
        public void Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            var incomingTransportMessage = new TransportMessage();

            fakeOutbox.ExistingMessage = new OutboxMessage { Id = incomingTransportMessage.Id };

            Invoke(incomingTransportMessage);

            Assert.True(fakeOutbox.ExistingMessage.Dispatched);
            Assert.Null(fakeOutbox.StoredMessage);
        }
    }
}