#nullable enable

namespace NServiceBus.AcceptanceTesting;

using System;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Outbox;

class AcceptanceTestingOutboxPersistence : Feature
{
    public AcceptanceTestingOutboxPersistence()
    {
        Enable<AcceptanceTestingTransactionalStorageFeature>();

        DependsOn<Outbox>();
        DependsOn<AcceptanceTestingTransactionalStorageFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var outboxStorage = new AcceptanceTestingOutboxStorage();

        context.Services.AddSingleton<IOutboxStorage>(outboxStorage);

        context.RegisterStartupTask(new OutboxCleaner(outboxStorage, TimeSpan.FromDays(5)));
    }

    class OutboxCleaner(AcceptanceTestingOutboxStorage storage, TimeSpan timeToKeepDeduplicationData)
        : FeatureStartupTask
    {
        protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
        {
            cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
        {
            if (cleanupTimer is null)
            {
                return Task.CompletedTask;
            }

            using (var waitHandle = new ManualResetEvent(false))
            {
                cleanupTimer.Dispose(waitHandle);

                waitHandle.WaitOne();
            }
            return Task.CompletedTask;
        }

        void PerformCleanup(object? state) => storage.RemoveEntriesOlderThan(DateTime.UtcNow - timeToKeepDeduplicationData);

        Timer? cleanupTimer;
    }
}