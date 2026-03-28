namespace NServiceBus.Persistence.InMemory;

using System;
using Features;
using Microsoft.Extensions.DependencyInjection;
using Outbox;

class InMemoryOutboxPersistence : Feature
{
    public InMemoryOutboxPersistence()
    {
        DependsOn<Features.Outbox>();
        DependsOn<InMemoryTransactionalStorageFeature>();

        Enable<InMemoryTransactionalStorageFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var outboxStorage = new InMemoryOutboxStorage();

        context.Services.AddSingleton<IOutboxStorage>(outboxStorage);

        var timeToKeepDeduplicationEntries = context.Settings.Get<TimeSpan>("Outbox.TimeToKeepDeduplicationEntries");
        context.RegisterStartupTask(new OutboxCleaner(outboxStorage, timeToKeepDeduplicationEntries));
    }
}
