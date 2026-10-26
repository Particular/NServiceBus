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

    public async Task<TSagaData?> Read<TSagaData>(Guid sagaId, SagaManifestCollection sagaManifests, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
    {
        var sagaStorageFile = await Open(sagaId, typeof(TSagaData), sagaManifests, cancellationToken)
            .ConfigureAwait(false);

        if (sagaStorageFile == null)
        {
            return null;
        }

        var (sagaData, timeouts) = await sagaStorageFile.Read<TSagaData>(cancellationToken)
            .ConfigureAwait(false);

        if (sagaData is not null)
        {
            sagaTimeouts[Key(sagaData)] = timeouts;
        }

        return sagaData;
    }

    // Timeouts are kept per saga and written with the saga file when the session completes
    public List<StoredSagaTimeout> GetTimeouts(IContainSagaData sagaData)
    {
        var key = Key(sagaData);
        if (!sagaTimeouts.TryGetValue(key, out var timeouts))
        {
            timeouts = [];
            sagaTimeouts[key] = timeouts;
        }

        return timeouts;
    }

    public void Update(IContainSagaData sagaData, SagaManifestCollection sagaManifests) => deferredActions.Add(new UpdateAction(sagaData, GetTimeouts(sagaData), sagaFiles, sagaManifests));

    public void Save(IContainSagaData sagaData, SagaManifestCollection sagaManifests) => deferredActions.Add(new SaveAction(sagaData, GetTimeouts(sagaData), sagaFiles, sagaManifests));

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

    static string Key(IContainSagaData sagaData) => $"{sagaData.GetType().FullName}{sagaData.Id}";

    readonly Dictionary<string, SagaStorageFile> sagaFiles = [];
    readonly Dictionary<string, List<StoredSagaTimeout>> sagaTimeouts = [];

    readonly List<StorageAction> deferredActions = [];
}