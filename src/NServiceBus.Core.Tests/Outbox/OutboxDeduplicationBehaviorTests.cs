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

     

        [Test]
        public void Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            var incomingTransportMessage = new TransportMessage();

            fakeOutbox.ExistingMessage = new OutboxMessage(incomingTransportMessage.Id);

            var context = new IncomingContext(null, incomingTransportMessage);

            Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }
    }
}