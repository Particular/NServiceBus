namespace NServiceBus.Features
{
    using System;
    using System.Threading;
    using NServiceBus.InMemory.Outbox;

    /// <summary>
    /// Used to configure in memory outbox persistence.
    /// </summary>
    public class InMemoryOutboxPersistence : Feature
    {
        internal const string TimeToKeepDeduplicationEntries = "Outbox.TimeToKeepDeduplicationEntries";

        internal InMemoryOutboxPersistence()
        {
            DependsOn<Outbox>();
            RegisterStartupTask<OutboxCleaner>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryOutboxStorage>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<OutboxCleaner>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.TimeToKeepDeduplicationData, context.Settings.Get<TimeSpan>(TimeToKeepDeduplicationEntries));
        }

        class OutboxCleaner : FeatureStartupTask
        {
            public InMemoryOutboxStorage InMemoryOutboxStorage { get; set; }

            public TimeSpan TimeToKeepDeduplicationData { get; set; }

            protected override void OnStart()
            {
                cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            }

            protected override void OnStop()
            {
                using (var waitHandle = new ManualResetEvent(false))
                {
                    cleanupTimer.Dispose(waitHandle);

                    waitHandle.WaitOne();
                }
            }

            void PerformCleanup(object state)
            {
                InMemoryOutboxStorage.RemoveEntriesOlderThan(DateTime.UtcNow - TimeToKeepDeduplicationData);
            }

// ReSharper disable once NotAccessedField.Local
            Timer cleanupTimer;
        }
    }
}