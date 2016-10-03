namespace NServiceBus.InMemory.SagaPersister
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;
    using NServiceBus.Sagas;
    using Saga;
    using Serializers.Json;

    /// <summary>
    /// In memory implementation of ISagaPersister for quick development.
    /// </summary>
    class InMemorySagaPersister : ISagaPersister
    {
        readonly SagaMetaModel sagaModel;
        ConcurrentDictionary<Guid, VersionedSagaEntity> data = new ConcurrentDictionary<Guid, VersionedSagaEntity>();
        ConcurrentDictionary<string, object> lockers = new ConcurrentDictionary<string, object>();

        public InMemorySagaPersister(SagaMetaModel sagaModel)
        {
            this.sagaModel = sagaModel;
        }

        public void Complete(IContainSagaData saga)
        {
            VersionedSagaEntity value;
            if (data.TryRemove(saga.Id, out value))
            {
                object lockToken;
                lockers.TryRemove(value.LockTokenKey, out lockToken);
            }
        }

        public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
        {
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
                return sagaData;
            }
            return default(TSagaData);
        }

        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            VersionedSagaEntity result;
            if (data.TryGetValue(sagaId, out result) && result?.SagaData is TSagaData)
            {
                var sagaData = result.Read<TSagaData>();
                return sagaData;
            }
            return default(TSagaData);
        }

        public void Save(IContainSagaData saga)
        {
            var lockenTokenKey = $"{saga.GetType().FullName}";
            var lockToken = lockers.GetOrAdd(lockenTokenKey, key => new object());
            lock (lockToken)
            {
                ValidateUniqueProperties(saga);

                data.AddOrUpdate(saga.Id,
                    id => new VersionedSagaEntity(saga, lockenTokenKey),
                    (id, original) => new VersionedSagaEntity(saga, lockenTokenKey, original)); // we can never end up here.
            }
        }

        public void Update(IContainSagaData saga)
        {
            Save(saga);
        }

        void ValidateUniqueProperties(IContainSagaData saga)
        {
            var sagaType = saga.GetType();
            var sagaMetaData = sagaModel.FindByEntityName(sagaType.FullName);

            if (sagaMetaData.CorrelationProperties.Count == 0) return;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var storedSaga in data)
            {
                if (storedSaga.Value.SagaData.GetType() != sagaType || (storedSaga.Key == saga.Id)) continue;

                foreach (var correlationProperty in sagaMetaData.CorrelationProperties)
                {
                    var uniqueProperty = saga.GetType().GetProperty(correlationProperty.Name);
                    if (!uniqueProperty.CanRead)
                    {
                        continue;
                    }

                    var inComingSagaPropertyValue = uniqueProperty.GetValue(saga, null);
                    var storedSagaPropertyValue = uniqueProperty.GetValue(storedSaga.Value.SagaData, null);
                    if (inComingSagaPropertyValue.Equals(storedSagaPropertyValue))
                    {
                        var message = $"Cannot store a saga. The saga with id '{storedSaga.Value.SagaData.Id}' already has property '{uniqueProperty}' with value '{storedSagaPropertyValue}'.";
                        throw new InvalidOperationException(message);
                    }
                }
            }
        }

        class VersionedSagaEntity
        {
            public VersionedSagaEntity(IContainSagaData sagaData, string lockTokenKey, VersionedSagaEntity original = null)
            {
                LockTokenKey = lockTokenKey;
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
                return (TSagaData)clone;
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
                return (IContainSagaData)serializer.DeserializeObject(json, source.GetType());
            }

            public IContainSagaData SagaData;
            public string LockTokenKey;

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
