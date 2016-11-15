namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Sagas;
    using Persistence;

    class InMemorySagaPersister : ISagaPersister
    {
        const string NoCorrelationId = "";
        readonly ConcurrentDictionary<Guid, Entry> _sagas = new ConcurrentDictionary<Guid, Entry>();
        readonly ConcurrentDictionary<string, Guid> _byCorrelationId = new ConcurrentDictionary<string, Guid>();
        static readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);

        class Entry
        {
            public string CorrelationId { get; private set; }
            public IContainSagaData ContainSagaData { get; }

            public Entry(IContainSagaData sagaData, string correlationId)
            {
                CorrelationId = correlationId;
                ContainSagaData = DeepClone(sagaData);
            }

            static IContainSagaData DeepClone(IContainSagaData source)
            {
                var json = serializer.SerializeObject(source);
                return (IContainSagaData)serializer.DeserializeObject(json, source.GetType());
            }

            public Entry UpdateTo(IContainSagaData sagaData)
            {
                return new Entry(sagaData, CorrelationId);
            }
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            ((InMemorySynchronizedStorageSession)session).Enlist(() =>
            {
                var entry = GetEntry(sagaData, context);

                if (_sagas.TryRemoveConditionally(sagaData.Id, entry) == false)
                {
                    throw new Exception("Saga can't be completed as it was updated by another process.");
                }

                // saga removed
                // clean the index
                if (entry.CorrelationId != NoCorrelationId)
                {
                    _byCorrelationId.TryRemoveConditionally(entry.CorrelationId, sagaData.Id);
                }
            });

            return Task.FromResult(0);
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : IContainSagaData
        {
            Entry value;

            if (_sagas.TryGetValue(sagaId, out value))
            {
                context.Set(sagaId.ToString(), value);

                if (value.ContainSagaData is TSagaData)
                {
                    return Task.FromResult((TSagaData)value.ContainSagaData);
                }
            }

            return Task.FromResult(default(TSagaData));
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var key = GetKey(typeof(TSagaData),new SagaCorrelationProperty(propertyName, propertyValue));
            Guid id;

            if (_byCorrelationId.TryGetValue(key, out id))
            {
                // this isn't updated atomically and may return null for an entry that has been indexed but not inserted yet
                return Get<TSagaData>(id, session, context);
            }

            return Task.FromResult(default(TSagaData));
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            ((InMemorySynchronizedStorageSession)session).Enlist(() =>
            {
                var correlationId = NoCorrelationId;
                if (correlationProperty != SagaCorrelationProperty.None)
                {
                    correlationId = GetKey(sagaData.GetType(),correlationProperty);
                    if (_byCorrelationId.TryAdd(correlationId, sagaData.Id) == false)
                    {
                        throw new InvalidOperationException($"The saga with the correlation id 'Name: {correlationProperty.Name} Value: {correlationProperty.Value}' already exists");
                    }
                }

                var entry = new Entry(sagaData, correlationId);
                if (_sagas.TryAdd(sagaData.Id, entry) == false)
                {
                    throw new Exception("FATAL: this should never happened as saga id should be unique");
                }

                context.Set(sagaData.Id.ToString(), entry);
            });

            return TaskEx.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            ((InMemorySynchronizedStorageSession)session).Enlist(() =>
            {
                var entry = GetEntry(sagaData, context);

                if (_sagas.TryUpdate(sagaData.Id, entry.UpdateTo(sagaData), entry) == false)
                {
                    throw new Exception($"InMemorySagaPersister concurrency violation: saga entity Id[{ sagaData.Id }] already saved.");
                }
            });

            return TaskEx.CompletedTask;
        }

        static Entry GetEntry(IContainSagaData sagaData, ContextBag context)
        {
            Entry entry;
            if (context.TryGet(sagaData.Id.ToString(), out entry) == false)
            {
                throw new Exception("The saga should be retrieved with Get method before it's updated");
            }
            return entry;
        }

        static string GetKey(Type sagaType, SagaCorrelationProperty correlationProperty)
        {
            return sagaType.FullName + "," +serializer.SerializeObject(correlationProperty);
        }
    }

    static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// Conditionally removes a value from a concurrent dictionary if it exists and is equal to <paramref name="previousValueToCompare"/>.
        /// </summary>
        /// <returns>Whether the value was removed.</returns>
        public static bool TryRemoveConditionally<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue previousValueToCompare)
        {
            // the only way to get the conditional removal working is to cast the dictionary down and use this ICollection method call.
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(new KeyValuePair<TKey, TValue>(key, previousValueToCompare));
        }
    }
}