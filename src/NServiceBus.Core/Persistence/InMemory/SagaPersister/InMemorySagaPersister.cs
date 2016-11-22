namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class InMemorySagaPersister : ISagaPersister
    {
        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            ((InMemorySynchronizedStorageSession)session).Enlist(() =>
           {
               var entry = GetEntry(sagaData, context);

               if (sagas.TryRemoveConditionally(sagaData.Id, entry) == false)
               {
                   throw new Exception("Saga can't be completed as it was updated by another process.");
               }

               // saga removed
               // clean the index
               if (Equals(entry.CorrelationId, NoCorrelationId) == false)
               {
                   byCorrelationId.TryRemoveConditionally(entry.CorrelationId, sagaData.Id);
               }
           });

            return TaskEx.CompletedTask;
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where TSagaData : IContainSagaData
        {
            Entry value;

            if (sagas.TryGetValue(sagaId, out value))
            {
                context.Set(sagaId.ToString(), value);

                var data = value.GetSagaCopy();
                return Task.FromResult((TSagaData)data);
            }

            return DefaultSagaDataTask<TSagaData>.Default;
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var key = new CorrelationId(typeof(TSagaData), propertyName, propertyValue);
            Guid id;

            if (byCorrelationId.TryGetValue(key, out id))
            {
                // this isn't updated atomically and may return null for an entry that has been indexed but not inserted yet
                return Get<TSagaData>(id, session, context);
            }

            return DefaultSagaDataTask<TSagaData>.Default;
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            ((InMemorySynchronizedStorageSession)session).Enlist(() =>
           {
               var correlationId = NoCorrelationId;
               if (correlationProperty != SagaCorrelationProperty.None)
               {
                   correlationId = GetKey(sagaData.GetType(), correlationProperty);
                   if (byCorrelationId.TryAdd(correlationId, sagaData.Id) == false)
                   {
                       throw new InvalidOperationException($"The saga with the correlation id 'Name: {correlationProperty.Name} Value: {correlationProperty.Value}' already exists");
                   }
               }

               var entry = new Entry(sagaData, correlationId);
               if (sagas.TryAdd(sagaData.Id, entry) == false)
               {
                   throw new Exception("A saga with this identifier already exists. This should never happened as saga identifier are meant to be unique.");
               }
           });

            return TaskEx.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            ((InMemorySynchronizedStorageSession)session).Enlist(() =>
           {
               var entry = GetEntry(sagaData, context);

               if (sagas.TryUpdate(sagaData.Id, entry.UpdateTo(sagaData), entry) == false)
               {
                   throw new Exception($"InMemorySagaPersister concurrency violation: saga entity Id[{sagaData.Id}] already saved.");
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

        static CorrelationId GetKey(Type sagaType, SagaCorrelationProperty correlationProperty)
        {
            var propertyName = correlationProperty.Name;
            var propertyValue = correlationProperty.Value;
            return new CorrelationId(sagaType, propertyName, propertyValue);
        }

        readonly ConcurrentDictionary<Guid, Entry> sagas = new ConcurrentDictionary<Guid, Entry>();
        readonly ConcurrentDictionary<CorrelationId, Guid> byCorrelationId = new ConcurrentDictionary<CorrelationId, Guid>();
        static readonly CorrelationId NoCorrelationId = new CorrelationId(typeof(object), "", new object());

        class Entry
        {
            public Entry(IContainSagaData sagaData, CorrelationId correlationId)
            {
                CorrelationId = correlationId;
                data = sagaData;
            }

            public CorrelationId CorrelationId { get; }

            static IContainSagaData DeepClone(IContainSagaData source)
            {
                var json = serializer.SerializeObject(source);
                return (IContainSagaData)serializer.DeserializeObject(json, source.GetType());
            }

            public IContainSagaData GetSagaCopy()
            {
                return DeepClone(data);
            }

            public Entry UpdateTo(IContainSagaData sagaData)
            {
                return new Entry(sagaData, CorrelationId);
            }

            readonly IContainSagaData data;
            static JsonMessageSerializer serializer = new JsonMessageSerializer(null);
        }

        /// <summary>
        /// This correlation id is cheap to create as type and the propertyName are not allocated (they are stored in the saga
        /// metadata).
        /// The only thing that is allocated is the correlationId itself and the propertyValue, which again, is allocated anyway
        /// by the saga behavior.
        /// </summary>
        class CorrelationId
        {
            public CorrelationId(Type type, string propertyName, object propertyValue)
            {
                this.type = type;
                this.propertyName = propertyName;
                this.propertyValue = propertyValue;
            }

            bool Equals(CorrelationId other)
            {
                return type == other.type && string.Equals(propertyName, other.propertyName) && propertyValue.Equals(other.propertyValue);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((CorrelationId)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    // propertyName isn't taken into consideration as there will be only one property per saga to correlate.
                    var hashCode = type.GetHashCode();
                    hashCode = (hashCode * 397) ^ propertyValue.GetHashCode();
                    return hashCode;
                }
            }

            readonly Type type;
            readonly string propertyName;
            readonly object propertyValue;
        }

        static class DefaultSagaDataTask<TSagaData>
            where TSagaData : IContainSagaData
        {
            public static Task<TSagaData> Default = Task.FromResult(default(TSagaData));
        }
    }

    static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// Conditionally removes a value from a concurrent dictionary if it exists and is equal to
        /// <paramref name="previousValueToCompare" />.
        /// </summary>
        /// <returns>Whether the value was removed.</returns>
        public static bool TryRemoveConditionally<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue previousValueToCompare)
        {
            // the only way to get the conditional removal working is to cast the dictionary down and use this ICollection method call.
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(new KeyValuePair<TKey, TValue>(key, previousValueToCompare));
        }
    }
}