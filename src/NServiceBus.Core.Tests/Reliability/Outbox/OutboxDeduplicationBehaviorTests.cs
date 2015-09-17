namespace NServiceBus.Core.Tests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
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
        [SetUp]
        public void SetUp()
        {
            fakeOutbox = new FakeOutboxStorage();
      
            behavior = new OutboxDeduplicationBehavior(fakeOutbox, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromSeconds(30)
            }, new FakeMessageDispatcher(), new DefaultDispatchStrategy());
        }

        [Test]
        public async Task Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            fakeOutbox.ExistingMessage = new OutboxMessage("id");

            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null));

            await Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public async Task Should_not_dispatch_the_message_if_handle_current_message_later_was_called()
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null))
            {
                handleCurrentMessageLaterWasCalled = true
            };

            await Invoke(context);

            Assert.False(fakeOutbox.WasDispatched);
        }

        Task Invoke(PhysicalMessageProcessingStageBehavior.Context context, bool shouldAbort = false)
        {
            return behavior.Invoke(context, () =>
            {
                if (shouldAbort)
                {
                    Assert.Fail("Pipeline should be aborted");
                }
                return Task.FromResult(0);
            });
        }

        FakeOutboxStorage fakeOutbox;
        OutboxDeduplicationBehavior behavior;
    }
}