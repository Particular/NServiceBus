namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Sagas;
    using NServiceBus.Serializers.Json;
    using NServiceBus.Utils;

    class InMemorySagaPersister : ISagaPersister
    {
        public Task Complete(IContainSagaData saga, ContextBag context)
        {
            VersionedSagaEntity value;
            data.TryRemove(saga.Id, out value);
            return TaskEx.Completed;
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, ContextBag context) where TSagaData : IContainSagaData
        {
            var values = data.Values.Where(x => x.SagaEntity is TSagaData);
            foreach (var entity in values)
            {
                var prop = typeof(TSagaData).GetProperty(propertyName);
                if (prop == null)
                {
                    continue;
                }
                if (!Equals(prop.GetValue(entity.SagaEntity, null), propertyValue))
                {
                    continue;
                }
                var clone = (TSagaData) DeepClone(entity.SagaEntity);
                entity.RecordRead(clone, version);
                return Task.FromResult(clone);
            }
            return Task.FromResult(default(TSagaData));
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, ContextBag context) where TSagaData : IContainSagaData
        {
            VersionedSagaEntity result;
            if (data.TryGetValue(sagaId, out result) && result?.SagaEntity is TSagaData)
            {
                var clone = (TSagaData) DeepClone(result.SagaEntity);
                result.RecordRead(clone, version);
                return Task.FromResult(clone);
            }
            return Task.FromResult(default(TSagaData));
        }

        public Task Save(IContainSagaData saga, IDictionary<string, object> correlationProperties, ContextBag context)
        {
            ValidateUniqueProperties(correlationProperties, saga);

            VersionedSagaEntity sagaEntity;
            if (data.TryGetValue(saga.Id, out sagaEntity))
            {
                sagaEntity.ConcurrencyCheck(saga, version);
            }

            data.AddOrUpdate(saga.Id, id => new VersionedSagaEntity
            {
                SagaEntity = DeepClone(saga)
            }, (id, original) => new VersionedSagaEntity
            {
                SagaEntity = DeepClone(saga),
                VersionCache = original.VersionCache
            });

            Interlocked.Increment(ref version);
            return TaskEx.Completed;
        }

        public Task Update(IContainSagaData saga, ContextBag context)
        {
            VersionedSagaEntity sagaEntity;
            if (data.TryGetValue(saga.Id, out sagaEntity))
            {
                sagaEntity.ConcurrencyCheck(saga, version);
            }

            data.AddOrUpdate(saga.Id, id => new VersionedSagaEntity
            {
                SagaEntity = DeepClone(saga)
            }, (id, original) => new VersionedSagaEntity
            {
                SagaEntity = DeepClone(saga),
                VersionCache = original.VersionCache
            });

            Interlocked.Increment(ref version);
            return TaskEx.Completed;
        }

        void ValidateUniqueProperties(IDictionary<string, object> correlationProperties, IContainSagaData saga)
        {
            var sagaType = saga.GetType();
            var existingSagas = (from s in data
                where s.Value.SagaEntity.GetType() == sagaType && (s.Key != saga.Id)
                select s.Value)
                .ToList();
            foreach (var correlationProperty in correlationProperties)
            {
                var uniqueProperty = sagaType.GetProperty(correlationProperty.Key);

                if (correlationProperty.Value == null)
                {
                    var message = $"Cannot store saga with id '{saga.Id}' since the unique property '{uniqueProperty.Name}' has a null value.";
                    throw new InvalidOperationException(message);
                }

                foreach (var storedSaga in existingSagas)
                {
                    var storedSagaPropertyValue = uniqueProperty.GetValue(storedSaga.SagaEntity, null);
                    if (Equals(correlationProperty.Value, storedSagaPropertyValue))
                    {
                        var message = $"Cannot store a saga. The saga with id '{storedSaga.SagaEntity.Id}' already has property '{uniqueProperty.Name}'.";
                        throw new InvalidOperationException(message);
                    }
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

            public IContainSagaData SagaEntity;

            public WeakKeyDictionary<IContainSagaData, int> VersionCache = new WeakKeyDictionary<IContainSagaData, int>();
        }
    }
}