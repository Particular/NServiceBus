namespace NServiceBus.Core.Tests.Pipeline
{
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class OutboxRecordBehaviorTests
    {
        [Test]
        public void Should_not_store_the_message_if_handle_current_message_later_was_called()
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null))
            {
                handleCurrentMessageLaterWasCalled = true
            };
            context.Set(new OutboxMessage("SomeId"));

            Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();

            behavior = new OutboxRecordBehavior
            {
                OutboxStorage = fakeOutbox
            };
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
        OutboxRecordBehavior behavior;
    }
}