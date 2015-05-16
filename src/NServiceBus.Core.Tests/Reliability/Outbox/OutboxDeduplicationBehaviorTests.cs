namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Transactions;
    using NServiceBus.Core.Tests.Timeout;
    using NServiceBus.Outbox;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class OutboxDeduplicationBehaviorTests
    {

        [Test]
        public void Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            fakeOutbox.ExistingMessage = new OutboxMessage("id");

            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null));

            Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public void Should_not_dispatch_the_message_if_handle_current_message_later_was_called()
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null))
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
      
            behavior = new OutboxDeduplicationBehavior(fakeOutbox, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromSeconds(30)
            }, new FakeMessageSender(),new DefaultDispatchStrategy());
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