namespace NServiceBus.AcceptanceTesting.AcceptanceTestingPersistence.Outbox
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Outbox;

    /// <summary>
    /// Used to configure in memory outbox persistence.
    /// </summary>
    class AcceptanceTestingOutboxPersistence : Feature
    {
        internal AcceptanceTestingOutboxPersistence()
        {
            DependsOn<Outbox>();
            Defaults(s => s.EnableFeature(typeof(AcceptanceTestingTransactionalStorageFeature)));
        }

        /// <summary>
        /// See <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var outboxStorage = new AcceptanceTestingOutboxStorage();

            context.Services.AddSingleton(typeof(IOutboxStorage), outboxStorage);

            var timeSpan = context.Settings.Get<TimeSpan>(TimeToKeepDeduplicationEntries);

            context.RegisterStartupTask(new OutboxCleaner(outboxStorage, timeSpan));
        }

        internal const string TimeToKeepDeduplicationEntries = "Outbox.TimeToKeepDeduplicationEntries";

        class OutboxCleaner : FeatureStartupTask
        {
            public OutboxCleaner(AcceptanceTestingOutboxStorage storage, TimeSpan timeToKeepDeduplicationData)
            {
                this.timeToKeepDeduplicationData = timeToKeepDeduplicationData;
                acceptanceTestingOutboxStorage = storage;
            }

            protected override Task OnStart(IMessageSession session)
            {
                cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                using (var waitHandle = new ManualResetEvent(false))
                {
                    cleanupTimer.Dispose(waitHandle);

                    // TODO: Use async synchronization primitive
                    waitHandle.WaitOne();
                }
                return Task.CompletedTask;
            }

            void PerformCleanup(object state)
            {
                acceptanceTestingOutboxStorage.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);
            }

            readonly AcceptanceTestingOutboxStorage acceptanceTestingOutboxStorage;
            readonly TimeSpan timeToKeepDeduplicationData;

// ReSharper disable once NotAccessedField.Local
            Timer cleanupTimer;
        }
    }
}