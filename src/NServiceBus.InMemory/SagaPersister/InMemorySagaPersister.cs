namespace NServiceBus.Persistence.InMemory;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;
using Sagas;

class InMemorySagaPersister(InMemorySagaPersisterSettings settings) : ISagaPersister
{
    public InMemorySagaPersister(InMemoryStorage storage, InMemorySagaPersisterSettings settings) : this(settings)
    {
        sagas = storage.Sagas;
        byCorrelationId = storage.SagaCorrelationIds;
    }

    public InMemorySagaPersister() : this(new InMemorySagaPersisterSettings(new JsonSerializerOptions()))
    {
    }

    public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var correlationId = correlationProperty != SagaCorrelationProperty.None
            ? new CorrelationId(sagaData.GetType(), correlationProperty)
            : NoCorrelationId;
        var entry = new SagaEntry(sagaData, correlationId, version: 1, settings.SerializerOptions);

        ((InMemorySynchronizedStorageSession)session).Enlist(
            new SaveOperationState(sagas, byCorrelationId, sagaData.Id, correlationId, entry),
            static state =>
            {
                if (!state.CorrelationId.Equals(NoCorrelationId)
                    && !state.ByCorrelationId.TryAdd(state.CorrelationId, state.SagaId))
                {
                    throw new InvalidOperationException($"The saga with the correlation id already exists");
                }

                if (!state.Sagas.TryAdd(state.SagaId, state.Entry))
                {
                    if (!state.CorrelationId.Equals(NoCorrelationId))
                    {
                        state.ByCorrelationId.TryRemove(new KeyValuePair<CorrelationId, Guid>(state.CorrelationId, state.SagaId));
                    }

                    throw new Exception("A saga with this identifier already exists. This should never happen as saga identifiers are meant to be unique.");
                }
            },
            static state =>
            {
                state.Sagas.TryRemove(new KeyValuePair<Guid, SagaEntry>(state.SagaId, state.Entry));

                if (!state.CorrelationId.Equals(NoCorrelationId))
                {
                    state.ByCorrelationId.TryRemove(new KeyValuePair<CorrelationId, Guid>(state.CorrelationId, state.SagaId));
                }
            });

        return Task.CompletedTask;
    }

    public Task<TSagaData> Get<TSagaData>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        where TSagaData : class, IContainSagaData
    {
        if (sagas.TryGetValue(sagaId, out var value))
        {
            SetEntry(context, sagaId, value);

            var data = value.GetSagaCopy();
            return Task.FromResult((TSagaData)data);
        }

        return CachedSagaDataTask<TSagaData>.Default!;
    }

    public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        where TSagaData : class, IContainSagaData
    {
        var key = new CorrelationId(typeof(TSagaData), propertyName, propertyValue);

        if (byCorrelationId.TryGetValue(key, out var id))
        {
            // This isn't updated atomically and may return null for an entry that has been indexed but not inserted yet
            return Get<TSagaData>(id, session, context, cancellationToken);
        }

        return CachedSagaDataTask<TSagaData>.Default!;
    }

    public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var entry = GetEntry(context, sagaData.Id);
        var updatedEntry = entry.UpdateTo(sagaData, settings.SerializerOptions);

        ((InMemorySynchronizedStorageSession)session).Enlist(
            new UpdateOperationState(sagas, sagaData.Id, entry, updatedEntry),
            static state =>
            {
                if (!state.Sagas.TryUpdate(state.SagaId, state.UpdatedEntry, state.Entry))
                {
                    throw new Exception($"InMemorySagaPersister concurrency violation: saga entity Id[{state.SagaId}] was modified by another process.");
                }
            },
            static state => state.Sagas.TryUpdate(state.SagaId, state.Entry, state.UpdatedEntry));

        return Task.CompletedTask;
    }

    public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        var entry = GetEntry(context, sagaData.Id);

        ((InMemorySynchronizedStorageSession)session).Enlist(
            new CompleteOperationState(sagas, byCorrelationId, sagaData.Id, entry),
            static state =>
            {
                if (!state.Sagas.TryRemove(new KeyValuePair<Guid, SagaEntry>(state.SagaId, state.Entry)))
                {
                    throw new Exception("Saga can't be completed as it was updated by another process.");
                }

                if (!state.Entry.CorrelationId.Equals(NoCorrelationId))
                {
                    state.ByCorrelationId.TryRemove(new KeyValuePair<CorrelationId, Guid>(state.Entry.CorrelationId, state.SagaId));
                }
            },
            static state =>
            {
                state.Sagas.TryAdd(state.SagaId, state.Entry);

                if (!state.Entry.CorrelationId.Equals(NoCorrelationId))
                {
                    state.ByCorrelationId.TryAdd(state.Entry.CorrelationId, state.SagaId);
                }
            });

        return Task.CompletedTask;
    }

    static void SetEntry(ContextBag context, Guid sagaId, SagaEntry value)
    {
        if (!context.TryGet(ContextKey, out Dictionary<Guid, SagaEntry>? entries))
        {
            entries = [];
            context.Set(ContextKey, entries);
        }
        entries[sagaId] = value;
    }

    static SagaEntry GetEntry(ContextBag context, Guid sagaDataId)
    {
        if (context.TryGet(ContextKey, out Dictionary<Guid, SagaEntry>? entries))
        {
            if (entries.TryGetValue(sagaDataId, out var entry))
            {
                return entry;
            }
        }
        throw new Exception("The saga should be retrieved with Get method before it's updated.");
    }

    ConcurrentDictionary<Guid, SagaEntry> sagas = new();
    ConcurrentDictionary<CorrelationId, Guid> byCorrelationId = new();

    readonly record struct SaveOperationState(
        ConcurrentDictionary<Guid, SagaEntry> Sagas,
        ConcurrentDictionary<CorrelationId, Guid> ByCorrelationId,
        Guid SagaId,
        CorrelationId CorrelationId,
        SagaEntry Entry);

    readonly record struct UpdateOperationState(
        ConcurrentDictionary<Guid, SagaEntry> Sagas,
        Guid SagaId,
        SagaEntry Entry,
        SagaEntry UpdatedEntry);

    readonly record struct CompleteOperationState(
        ConcurrentDictionary<Guid, SagaEntry> Sagas,
        ConcurrentDictionary<CorrelationId, Guid> ByCorrelationId,
        Guid SagaId,
        SagaEntry Entry);

    const string ContextKey = "NServiceBus.InMemoryPersistence.Sagas";
    static readonly CorrelationId NoCorrelationId = new CorrelationId(typeof(object), "", new object());
}
