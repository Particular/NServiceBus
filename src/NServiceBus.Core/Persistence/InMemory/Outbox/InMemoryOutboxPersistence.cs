namespace NServiceBus.InMemory.Outbox
{
    using System;
    using System.Threading;
    using Features;

    /// <summary>
    /// Used to configure in memory outbox persistence.
    /// </summary>
    public class InMemoryOutboxPersistence : Feature
    {
        internal InMemoryOutboxPersistence()
        {
            DependsOn<Outbox>();
            RegisterStartupTask<OutboxCleaner>();
        }

        /// <summary>
        /// See <see cref="Feature.Setup"/>
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<InMemoryOutboxStorage>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<OutboxCleaner>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(t => t.TimeToKeepDeduplicationData, context.Settings.Get<TimeSpan>(Outbox.TimeToKeepDeduplicationEntries));
        }

        class OutboxCleaner:FeatureStartupTask
        {
            public InMemoryOutboxStorage InMemoryOutboxStorage { get; set; }

            public TimeSpan TimeToKeepDeduplicationData { get; set; }
            protected override void OnStart()
            {
                cleanupTimer = new Timer(PerformCleanup,null,TimeSpan.FromMinutes(1),TimeSpan.FromMinutes(1));
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