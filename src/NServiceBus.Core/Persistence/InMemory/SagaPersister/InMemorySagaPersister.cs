namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;
    using NServiceBus.Serializers.Json;

    class InMemorySagaPersister : ISagaPersister
    {
        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var inMemSession = (InMemorySynchronizedStorageSession)session;
            inMemSession.Enlist(() =>
            {
                VersionedSagaEntity value;
                data.TryRemove(sagaData.Id, out value);
            });
            return TaskEx.Completed;
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            Guard.AgainstNull(nameof(propertyValue), propertyValue);

            var values = data.Values.Where(x => x.SagaData is TSagaData);
            foreach (var entity in values)
            {
                var prop = typeof(TSagaData).GetProperty(propertyName);
                if (prop == null)
                {
                    continue;
                }
                var existingValue = prop.GetValue(entity.SagaData);

                if (existingValue.ToString() != propertyValue.ToString())
                {
                    continue;
                }
                var clone = (TSagaData) DeepClone(entity.SagaData);
                entity.RecordRead(clone, version);
                return Task.FromResult(clone);
            }
            return Task.FromResult(default(TSagaData));
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            VersionedSagaEntity result;
            if (data.TryGetValue(sagaId, out result) && result?.SagaData is TSagaData)
            {
                var clone = (TSagaData) DeepClone(result.SagaData);
                result.RecordRead(clone, version);
                return Task.FromResult(clone);
            }
            return Task.FromResult(default(TSagaData));
        }

        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var inMemSession = (InMemorySynchronizedStorageSession) session;
            inMemSession.Enlist(() =>
            {
                if (correlationProperty != SagaCorrelationProperty.None)
                {
                    ValidateUniqueProperties(correlationProperty, sagaData);
                }

                VersionedSagaEntity sagaEntity;
                if (data.TryGetValue(sagaData.Id, out sagaEntity))
                {
                    sagaEntity.ConcurrencyCheck(sagaData, version);
                }

                data.AddOrUpdate(sagaData.Id, id => new VersionedSagaEntity
                {
                    SagaData = DeepClone(sagaData)
                }, (id, original) => new VersionedSagaEntity
                {
                    SagaData = DeepClone(sagaData),
                    VersionCache = original.VersionCache
                });

                Interlocked.Increment(ref version);
            });
            return TaskEx.Completed;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var inMemSession = (InMemorySynchronizedStorageSession)session;
            inMemSession.Enlist(() =>
            {
                VersionedSagaEntity sagaEntity;
                if (data.TryGetValue(sagaData.Id, out sagaEntity))
                {
                    sagaEntity.ConcurrencyCheck(sagaData, version);
                }

                data.AddOrUpdate(sagaData.Id, id => new VersionedSagaEntity
                {
                    SagaData = DeepClone(sagaData)
                }, (id, original) => new VersionedSagaEntity
                {
                    SagaData = DeepClone(sagaData),
                    VersionCache = original.VersionCache
                });

                Interlocked.Increment(ref version);
            });
            return TaskEx.Completed;
        }

        void ValidateUniqueProperties(SagaCorrelationProperty correlationProperty, IContainSagaData saga)
        {
            var sagaType = saga.GetType();
            var existingSagas = (from s in data
                where s.Value.SagaData.GetType() == sagaType && (s.Key != saga.Id)
                select s.Value)
                .ToList();
            var uniqueProperty = sagaType.GetProperty(correlationProperty.Name);

            if (correlationProperty.Value == null)
            {
                var message = $"Cannot store saga with id '{saga.Id}' since the unique property '{uniqueProperty.Name}' has a null value.";
                throw new InvalidOperationException(message);
            }

            foreach (var storedSaga in existingSagas)
            {
                var storedSagaPropertyValue = uniqueProperty.GetValue(storedSaga.SagaData, null);
                if (Equals(correlationProperty.Value, storedSagaPropertyValue))
                {
                    var message = $"Cannot store a saga. The saga with id '{storedSaga.SagaData.Id}' already has property '{uniqueProperty.Name}'.";
                    throw new InvalidOperationException(message);
                }
            }
        }

        IContainSagaData DeepClone(IContainSagaData source)
        {
            var json = serializer.SerializeObject(source);
            return (IContainSagaData) serializer.DeserializeObject(json, source.GetType());
        }

        ConcurrentDictionary<Guid, VersionedSagaEntity> data = new ConcurrentDictionary<Guid, VersionedSagaEntity>();
        JsonMessageSerializer serializer = new JsonMessageSerializer(null);

        int version;

        class VersionedSagaEntity
        {
            public void RecordRead(IContainSagaData sagaEntity, int currentVersion)
            {
                VersionCache[sagaEntity] = currentVersion;
            }

            public void ConcurrencyCheck(IContainSagaData sagaEntity, int currentVersion)
            {
                int v;
                if (!VersionCache.TryGetValue(sagaEntity, out v))
                {
                    throw new Exception($"InMemorySagaPersister in an inconsistent state: entity Id[{sagaEntity.Id}] not read.");
                }

                if (v != currentVersion)
                {
                    throw new Exception($@"InMemorySagaPersister concurrency violation: saga entity Id[{sagaEntity.Id}] already saved.");
                }
            }

            public IContainSagaData SagaData;

            public WeakKeyDictionary<IContainSagaData, int> VersionCache = new WeakKeyDictionary<IContainSagaData, int>();
        }
    }
}