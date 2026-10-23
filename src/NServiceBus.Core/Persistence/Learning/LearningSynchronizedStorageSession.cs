#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Outbox;
using Persistence;
using Sagas;
using Transport;

class LearningSynchronizedStorageSession : ICompletableSynchronizedStorageSession
{
    public void Dispose()
    {
        if (sagaFiles.Count == 0)
        {
            return;
        }

        foreach (var sagaFile in sagaFiles.Values)
        {
            sagaFile.Dispose();
        }

        sagaFiles.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        if (sagaFiles.Count == 0)
        {
            return;
        }

        foreach (var sagaFile in sagaFiles.Values)
        {
            await sagaFile.DisposeAsync().ConfigureAwait(false);
        }

        sagaFiles.Clear();
    }

    public ValueTask<bool> TryOpen(IOutboxTransaction transaction, ContextBag context,
        CancellationToken cancellationToken = default) =>
        new(false);

    public ValueTask<bool> TryOpen(TransportTransaction transportTransaction, ContextBag context,
        CancellationToken cancellationToken = default) =>
        new(false);

    public Task Open(ContextBag context, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var action in deferredActions)
        {
            await action.Execute(cancellationToken).ConfigureAwait(false);
        }
        deferredActions.Clear();
    }

    public async Task<(TSagaData? SagaData, List<StoredSagaTimeout> Timeouts)> Read<TSagaData>(Guid sagaId, SagaManifestCollection sagaManifests, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
    {
        var sagaStorageFile = await Open(sagaId, typeof(TSagaData), sagaManifests, cancellationToken)
            .ConfigureAwait(false);

        if (sagaStorageFile == null)
        {
            return (null, []);
        }

        return await sagaStorageFile.Read<TSagaData>(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Update(IContainSagaData sagaData, IReadOnlyList<StoredSagaTimeout> timeouts, SagaManifestCollection sagaManifests) => deferredActions.Add(new UpdateAction(sagaData, timeouts, sagaFiles, sagaManifests));

    public void Save(IContainSagaData sagaData, IReadOnlyList<StoredSagaTimeout> timeouts, SagaManifestCollection sagaManifests) => deferredActions.Add(new SaveAction(sagaData, timeouts, sagaFiles, sagaManifests));

    public void Complete(IContainSagaData sagaData, SagaManifestCollection sagaManifests) => deferredActions.Add(new CompleteAction(sagaData, sagaFiles, sagaManifests));

    async Task<SagaStorageFile?> Open(Guid sagaId, Type entityType, SagaManifestCollection sagaManifests, CancellationToken cancellationToken)
    {
        var sagaManifest = sagaManifests.GetForEntityType(entityType);

        var sagaStorageFile = await SagaStorageFile.Open(sagaId, sagaManifest, cancellationToken)
            .ConfigureAwait(false);

        if (sagaStorageFile != null)
        {
            sagaFiles.RegisterSagaFile(sagaStorageFile, sagaId, sagaManifest.SagaEntityType);
        }

        return sagaStorageFile;
    }

    readonly Dictionary<string, SagaStorageFile> sagaFiles = [];

    readonly List<StorageAction> deferredActions = [];
}