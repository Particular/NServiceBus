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
        var configuredStorage = context.Settings.GetOrDefault<InMemoryStorage>(InMemoryStorageRuntime.StorageKey);
        InMemoryStorageRuntime.Configure(context.Services, configuredStorage);

        context.Services.AddSingleton<InMemoryOutboxStorage>(sp => new InMemoryOutboxStorage(sp.GetRequiredService<InMemoryStorage>()));
        context.Services.AddSingleton<IOutboxStorage>(sp => sp.GetRequiredService<InMemoryOutboxStorage>());

        var timeToKeepDeduplicationEntries = context.Settings.Get<TimeSpan>("Outbox.TimeToKeepDeduplicationEntries");
        context.RegisterStartupTask(sp => new OutboxCleaner(sp.GetRequiredService<InMemoryOutboxStorage>(), timeToKeepDeduplicationEntries));
    }
}