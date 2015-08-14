﻿namespace NServiceBus.Core.Tests.Reliability.Outbox
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

        [Test]
        public async void Should_shortcut_the_pipeline_if_existing_message_is_found()
        {
            fakeOutbox.ExistingMessage = new OutboxMessage("id");

            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null));

            await Invoke(context);

            Assert.Null(fakeOutbox.StoredMessage);
        }

        [Test]
        public async void Should_not_dispatch_the_message_if_handle_current_message_later_was_called()
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new IncomingMessage("id", new Dictionary<string, string>(), new MemoryStream()), null))
            {
                handleCurrentMessageLaterWasCalled = true
            };

            await Invoke(context);

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

        Task Invoke(PhysicalMessageProcessingStageBehavior.Context context, bool shouldAbort = false)
        {
            return behavior.Invoke(context, () =>
            {
                if (shouldAbort)
                {
                    Assert.Fail("Pipeline should be aborted");
                }

                return Task.FromResult(true);
            });
        }

        FakeOutboxStorage fakeOutbox;
        OutboxDeduplicationBehavior behavior;
    }
}