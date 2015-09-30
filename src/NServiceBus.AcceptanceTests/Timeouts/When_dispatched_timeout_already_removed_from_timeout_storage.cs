namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Transactions;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    public class When_timeout_already_removed : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_rollback_and_not_deliver_timeout_when_using_dtc()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b
                    .CustomConfig(configure => configure.Transactions().EnableDistributedTransactions())
                    .Given(bus =>
                    {
                        bus.Defer(TimeSpan.FromSeconds(5), new MyMessage());
                    }))
                .Done(c => c.AttemptedToRemoveTimeout || c.MessageReceived)
                .Run();

            Assert.IsFalse(context.MessageReceived, "Message should not be delivered using dtc.");
            Assert.AreEqual(2, context.NumberOfProcessingAttempts, "The rollback should cause a retry.");
            Assert.IsTrue(context.AttemptedToRemoveTimeout);
        }

        [Test]
        public void Should_rollback_and_deliver_timeout_anyway_when_using_native_tx()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b
                    .CustomConfig(configure => configure.Transactions().DisableDistributedTransactions())
                    .Given(bus =>
                    {
                        bus.Defer(TimeSpan.FromSeconds(5), new MyMessage());
                    }))
                .Done(c => c.AttemptedToRemoveTimeout && c.MessageReceived)
                .Run();

            Assert.IsTrue(context.MessageReceived, "Message should be delivered although transaction was aborted.");
            Assert.AreEqual(2, context.NumberOfProcessingAttempts, "The rollback should cause a retry.");
            Assert.IsTrue(context.AttemptedToRemoveTimeout);
        }

        [Test]
        public void Should_deliver_timeout_anyway_when_using_no_tx()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(b => b
                    .CustomConfig(configure => configure.Transactions().Disable())
                    .Given(bus =>
                    {
                        bus.Defer(TimeSpan.FromSeconds(5), new MyMessage());
                    }))
                .Done(c => c.AttemptedToRemoveTimeout && c.MessageReceived)
                .Run();

            Assert.IsTrue(context.MessageReceived, "Message should be delivered although timeout processing fails.");
            Assert.AreEqual(1, context.NumberOfProcessingAttempts, "Should not retry without transactions enabled.");
            Assert.IsTrue(context.AttemptedToRemoveTimeout);
        }

        public class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
            public bool AttemptedToRemoveTimeout { get; set; }
            public int NumberOfProcessingAttempts { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
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

            public class EndpointConfiguration : IWantToRunBeforeConfigurationIsFinalized
            {
                public static IBuilder builder;

                public void Run(Configure config)
                {
                    builder = config.Builder;
                }
            }

            public class DispatcherInterceptor : Feature
            {
                public DispatcherInterceptor()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    var originalPersister = EndpointConfiguration.builder.Build<IPersistTimeouts>();
                    var ctx = EndpointConfiguration.builder.Build<Context>();
                    context.Container.ConfigureComponent(() => new TimeoutPersistanceWrapper(originalPersister, originalPersister as IPersistTimeoutsV2, ctx), DependencyLifecycle.SingleInstance);
                }
            }

            class TimeoutPersistanceWrapper : IPersistTimeouts, IPersistTimeoutsV2
            {
                IPersistTimeouts originalTimeoutPersister;
                IPersistTimeoutsV2 originalTimeoutPersisterV2;
                Context context;

                public TimeoutPersistanceWrapper(IPersistTimeouts originalTimeoutPersister, IPersistTimeoutsV2 originalTimeoutPersisterV2, Context context)
                {
                    this.originalTimeoutPersister = originalTimeoutPersister;
                    this.originalTimeoutPersisterV2 = originalTimeoutPersisterV2;
                    this.context = context;
                }

                public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
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

                    using (var tx = new TransactionScope(TransactionScopeOption.Suppress))
                    { 
                        // delete the timeout so it won't be available on retries
                        originalTimeoutPersisterV2.TryRemove(timeoutId);
                        tx.Complete();
                    }

                    return false;
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage { }
    }
}
