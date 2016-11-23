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
        public InMemorySagaPersister()
        {
            sagas = new ConcurrentDictionary<Guid, Entry>();
            sagasCollection = sagas;
            byCorrelationId = new ConcurrentDictionary<CorrelationId, Guid>();
            byCorrelationIdCollection = byCorrelationId;
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            ((InMemorySynchronizedStorageSession)session).Enlist(() =>
           {
               var entry = GetEntry(context, sagaData.Id);

               if (sagasCollection.Remove(new KeyValuePair<Guid, Entry>(sagaData.Id, entry)) == false)
               {
                   throw new Exception("Saga can't be completed as it was updated by another process.");
               }

                // saga removed
                // clean the index
                if (Equals(entry.CorrelationId, NoCorrelationId) == false)
               {
                   byCorrelationIdCollection.Remove(new KeyValuePair<CorrelationId, Guid>(entry.CorrelationId, sagaData.Id));
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
                SetEntry(context, sagaId, value);

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
                   correlationId = new CorrelationId(sagaData.GetType(), correlationProperty);
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
               var entry = GetEntry(context, sagaData.Id);

               if (sagas.TryUpdate(sagaData.Id, entry.UpdateTo(sagaData), entry) == false)
               {
                   throw new Exception($"InMemorySagaPersister concurrency violation: saga entity Id[{sagaData.Id}] already saved.");
               }
           });

            return TaskEx.CompletedTask;
        }

        static void SetEntry(ContextBag context, Guid sagaId, Entry value)
        {
            Dictionary<Guid, Entry> entries;
            if (context.TryGet(ContextKey, out entries) == false)
            {
                entries = new Dictionary<Guid, Entry>();
                context.Set(ContextKey, entries);
            }
            entries[sagaId] = value;
        }

        static Entry GetEntry(ReadOnlyContextBag context, Guid sagaDataId)
        {
            Dictionary<Guid, Entry> entries;
            if (context.TryGet(ContextKey, out entries))
            {
                Entry entry;

                if (entries.TryGetValue(sagaDataId, out entry))
                {
                    return entry;
                }
            }
            throw new Exception("The saga should be retrieved with Get method before it's updated");
        }

        readonly ConcurrentDictionary<Guid, Entry> sagas;
        readonly ConcurrentDictionary<CorrelationId, Guid> byCorrelationId;
        readonly ICollection<KeyValuePair<Guid, Entry>> sagasCollection;
        readonly ICollection<KeyValuePair<CorrelationId, Guid>> byCorrelationIdCollection;
        const string ContextKey = "NServiceBus.InMemorySagaPersistence.Sagas";
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

            public CorrelationId(Type sagaType, SagaCorrelationProperty correlationProperty)
                : this(sagaType, correlationProperty.Name, correlationProperty.Value)
            {
            }

            bool Equals(CorrelationId other)
            {
                return type == other.type && String.Equals(propertyName, other.propertyName) && propertyValue.Equals(other.propertyValue);
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
}