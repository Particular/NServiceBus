namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    public class When_dispatched_timeout_already_removed_from_timeout_storage
    {
        [Test]
        public void Should_rollback_and_not_deliver_timeout_when_using_dtc()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<TimeoutHandlingEndpoint>(b => b
                    .CustomConfig(configure => Configure.Transactions.Advanced(s => s.EnableDistributedTransactions()))
                    .Given(bus =>
                    {
                        bus.Defer(TimeSpan.FromSeconds(5), new MyMessage());
                    }))
                .Done(c => c.AttemptedToRemoveTimeout || c.MessageReceived)
                .Run();

            Assert.IsFalse(context.MessageReceived, "Message should not be delivered using dtc");
            Assert.AreEqual(2, context.NumberOfProcessingAttempts, "The rollback should cause a retry");
            Assert.IsTrue(context.AttemptedToRemoveTimeout);
        }

        [Test]
        public void Should_rollback_and_deliver_timeout_anyway_when_using_native_tx()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<TimeoutHandlingEndpoint>(b => b
                    .CustomConfig(configure => Configure.Transactions.Advanced(s => s.DisableDistributedTransactions()))
                    .Given(bus =>
                    {
                        bus.Defer(TimeSpan.FromSeconds(5), new MyMessage());
                    }))
                .Done(c => c.AttemptedToRemoveTimeout && c.MessageReceived)
                .Run();

            Assert.IsTrue(context.MessageReceived, "Message should only be delivered although transaction has been aborted");
            Assert.AreEqual(2, context.NumberOfProcessingAttempts, "The rollback should cause a retry");
            Assert.IsTrue(context.AttemptedToRemoveTimeout);
        }

        [Test]
        public void Should_deliver_timeout_anyway_when_using_no_tx()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<TimeoutHandlingEndpoint>(b => b
                    .CustomConfig(configure => Configure.Transactions.Disable())
                    .Given(bus =>
                    {
                        bus.Defer(TimeSpan.FromSeconds(5), new MyMessage());
                    }))
                .Done(c => c.AttemptedToRemoveTimeout && c.MessageReceived)
                .Run();

            Assert.IsTrue(context.MessageReceived, "Message should be delivered although timeout processing fails");
            Assert.AreEqual(1, context.NumberOfProcessingAttempts, "Should not retry without transactions enabled");
            Assert.IsTrue(context.AttemptedToRemoveTimeout);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }

            public bool AttemptedToRemoveTimeout { get; set; }

            public int NumberOfProcessingAttempts { get; set; }
        }

        public class TimeoutHandlingEndpoint : EndpointConfigurationBuilder
        {
            public TimeoutHandlingEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class DelayedMessageHandler : IHandleMessages<MyMessage>
            {
                Context context;

                public DelayedMessageHandler(Context context)
                {
                    this.context = context;
                }

                public void Handle(MyMessage message)
                {
                    context.MessageReceived = true;
                }
            }

            public class EndpointConfiguration : IWantToRunWhenConfigurationIsComplete
            {
                Context context;
                IPersistTimeouts originalPersister;

                public EndpointConfiguration(Context context, IPersistTimeouts originalPersister)
                {
                    this.context = context;
                    this.originalPersister = originalPersister;
                }

                public void Run()
                {
                    Configure.Component(b => new TimeoutPersistenceWrapper(originalPersister, originalPersister as IPersistTimeoutsV2, context), DependencyLifecycle.SingleInstance);
                }
            }

            class TimeoutPersistenceWrapper : IPersistTimeouts, IPersistTimeoutsV2
            {
                IPersistTimeouts originalTimeoutPersister;
                IPersistTimeoutsV2 originalTimeoutPersisterV2;
                Context context;

                public TimeoutPersistenceWrapper(IPersistTimeouts originalTimeoutPersister, IPersistTimeoutsV2 originalTimeoutPersisterV2, Context context)
                {
                    this.originalTimeoutPersister = originalTimeoutPersister;
                    this.originalTimeoutPersisterV2 = originalTimeoutPersisterV2;
                    this.context = context;
                }

                public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
                {
                    return originalTimeoutPersister.GetNextChunk(startSlice, out nextTimeToRunQuery);
                }

                public void Add(TimeoutData timeout)
                {
                    originalTimeoutPersister.Add(timeout);
                }

                public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
                {
                    return originalTimeoutPersister.TryRemove(timeoutId, out timeoutData);
                }

                public void RemoveTimeoutBy(Guid sagaId)
                {
                    originalTimeoutPersister.RemoveTimeoutBy(sagaId);
                }

                public TimeoutData Peek(string timeoutId)
                {
                    context.NumberOfProcessingAttempts++;
                    return originalTimeoutPersisterV2.Peek(timeoutId);
                }

                public bool TryRemove(string timeoutId)
                {
                    context.AttemptedToRemoveTimeout = true;
                    // delete the timeout so it won't be available on retries
                    originalTimeoutPersisterV2.TryRemove(timeoutId);
                    return false;
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }
    }
}