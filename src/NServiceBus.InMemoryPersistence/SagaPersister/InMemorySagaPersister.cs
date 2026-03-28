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
    public InMemorySagaPersister() : this(new InMemorySagaPersisterSettings(new JsonSerializerOptions()))
    {
    }

    public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        ((InMemorySynchronizedStorageSession)session).Enlist(() =>
        {
            var correlationId = NoCorrelationId;
            if (correlationProperty != SagaCorrelationProperty.None)
            {
                correlationId = new CorrelationId(sagaData.GetType(), correlationProperty);
                if (!byCorrelationId.TryAdd(correlationId, sagaData.Id))
                {
                    throw new InvalidOperationException($"The saga with the correlation id 'Name: {correlationProperty.Name} Value: {correlationProperty.Value}' already exists");
                }
            }

            var entry = new SagaEntry(sagaData, correlationId, version: 1, settings.SerializerOptions);
            if (!sagas.TryAdd(sagaData.Id, entry))
            {
                throw new Exception("A saga with this identifier already exists. This should never happen as saga identifiers are meant to be unique.");
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
        ((InMemorySynchronizedStorageSession)session).Enlist(() =>
        {
            var entry = GetEntry(context, sagaData.Id);

            if (!sagas.TryUpdate(sagaData.Id, entry.UpdateTo(sagaData, settings.SerializerOptions), entry))
            {
                throw new Exception($"InMemorySagaPersister concurrency violation: saga entity Id[{sagaData.Id}] was modified by another process.");
            }
        });

        return Task.CompletedTask;
    }

    public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        ((InMemorySynchronizedStorageSession)session).Enlist(() =>
        {
            var entry = GetEntry(context, sagaData.Id);

            if (!sagas.TryRemove(new KeyValuePair<Guid, SagaEntry>(sagaData.Id, entry)))
            {
                throw new Exception("Saga can't be completed as it was updated by another process.");
            }

            // Saga removed, clean the index
            if (!entry.CorrelationId.Equals(NoCorrelationId))
            {
                byCorrelationId.TryRemove(new KeyValuePair<CorrelationId, Guid>(entry.CorrelationId, sagaData.Id));
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

    readonly ConcurrentDictionary<Guid, SagaEntry> sagas = new();
    readonly ConcurrentDictionary<CorrelationId, Guid> byCorrelationId = new();

    const string ContextKey = "NServiceBus.InMemoryPersistence.Sagas";
    static readonly CorrelationId NoCorrelationId = new CorrelationId(typeof(object), "", new object());
}