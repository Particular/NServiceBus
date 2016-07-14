namespace NServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class InMemorySagaPersister : ISagaPersister
    {
        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var inMemSession = (InMemorySynchronizedStorageSession) session;
            inMemSession.Enlist(() =>
            {
                VersionedSagaEntity value;
                data.TryRemove(sagaData.Id, out value);
            });
            return TaskEx.CompletedTask;
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            Guard.AgainstNull(nameof(propertyValue), propertyValue);

            foreach (var entity in data.Values)
            {
                if (!(entity.SagaData is TSagaData))
                {
                    continue;
                }

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
                var sagaData = entity.Read<TSagaData>();
                return Task.FromResult(sagaData);
            }
            return Task.FromResult(default(TSagaData));
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            VersionedSagaEntity result;
            if (data.TryGetValue(sagaId, out result) && result?.SagaData is TSagaData)
            {
                var sagaData = result.Read<TSagaData>();
                return Task.FromResult(sagaData);
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

                data.AddOrUpdate(sagaData.Id,
                    id => new VersionedSagaEntity(sagaData),
                    (id, original) => new VersionedSagaEntity(sagaData, original));
            });
            return TaskEx.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var inMemSession = (InMemorySynchronizedStorageSession) session;
            inMemSession.Enlist(() =>
            {
                data.AddOrUpdate(sagaData.Id,
                    id => new VersionedSagaEntity(sagaData),
                    (id, original) => new VersionedSagaEntity(sagaData, original));
            });
            return TaskEx.CompletedTask;
        }

        void ValidateUniqueProperties(SagaCorrelationProperty correlationProperty, IContainSagaData saga)
        {
            var sagaType = saga.GetType();
            var existingSagas = new List<VersionedSagaEntity>();
            foreach (var s in data)
            {
                if (s.Value.SagaData.GetType() == sagaType && (s.Key != saga.Id))
                {
                    existingSagas.Add(s.Value);
                }
            }
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

        ConcurrentDictionary<Guid, VersionedSagaEntity> data = new ConcurrentDictionary<Guid, VersionedSagaEntity>();

        class VersionedSagaEntity
        {
            public VersionedSagaEntity(IContainSagaData sagaData, VersionedSagaEntity original = null)
            {
                SagaData = DeepClone(sagaData);
                if (original != null)
                {
                    original.ConcurrencyCheck(sagaData);

                    versionCache = original.versionCache;
                    version = original.version;
                    version++;
                }
                else
                {
                    versionCache = new ConditionalWeakTable<IContainSagaData, SagaVersion>();
                    versionCache.Add(sagaData, new SagaVersion(version));
                }
            }

            public TSagaData Read<TSagaData>()
                where TSagaData : IContainSagaData
            {
                var clone = DeepClone(SagaData);
                versionCache.Add(clone, new SagaVersion(version));
                return (TSagaData) clone;
            }

            void ConcurrencyCheck(IContainSagaData sagaEntity)
            {
                SagaVersion v;
                if (!versionCache.TryGetValue(sagaEntity, out v))
                {
                    throw new Exception($"InMemorySagaPersister in an inconsistent state: entity Id[{sagaEntity.Id}] not read.");
                }

                if (v.Version != version)
                {
                    throw new Exception($"InMemorySagaPersister concurrency violation: saga entity Id[{sagaEntity.Id}] already saved.");
                }
            }

            static IContainSagaData DeepClone(IContainSagaData source)
            {
                var json = serializer.SerializeObject(source);
                return (IContainSagaData) serializer.DeserializeObject(json, source.GetType());
            }

            public IContainSagaData SagaData;

            ConditionalWeakTable<IContainSagaData, SagaVersion> versionCache;

            int version;

            static JsonMessageSerializer serializer = new JsonMessageSerializer(null);

            class SagaVersion
            {
                public SagaVersion(long version)
                {
                    Version = version;
                }

                public long Version;
            }
        }
    }
}