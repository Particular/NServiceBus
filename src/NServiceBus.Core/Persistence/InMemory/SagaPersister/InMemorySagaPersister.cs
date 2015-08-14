namespace NServiceBus.InMemory.SagaPersister
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Saga;
    using NServiceBus.Utils;
    using Serializers.Json;

    /// <summary>
    /// In memory implementation of ISagaPersister for quick development.
    /// </summary>
    class InMemorySagaPersister : ISagaPersister
    {
        int version;
        JsonMessageSerializer serializer = new JsonMessageSerializer(null);
        ConcurrentDictionary<string, VersionedSagaEntity> data = new ConcurrentDictionary<string, VersionedSagaEntity>();

        public void Complete(IContainSagaData saga, SagaPersistenceOptions options)
        {
            VersionedSagaEntity value;
            data.TryRemove(saga.Id.ToString(), out value);
        }

        public TSagaData Get<TSagaData>(string propertyName, object propertyValue, SagaPersistenceOptions options) where TSagaData : IContainSagaData
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
                var clone = (TSagaData)DeepClone(entity.SagaEntity);
                entity.RecordRead(clone, version);
                return clone;
            }
            return default(TSagaData);
        }

        public TSagaData Get<TSagaData>(string sagaId, SagaPersistenceOptions options) where TSagaData : IContainSagaData
        {
            VersionedSagaEntity result;
            if (data.TryGetValue(sagaId, out result) && (result != null) && (result.SagaEntity is TSagaData))
            {
                var clone = (TSagaData)DeepClone(result.SagaEntity);
                result.RecordRead(clone, version);
                return clone;
            }
            return default(TSagaData);
        }

        public void Save(IContainSagaData saga, SagaPersistenceOptions options)
        {
            ValidateUniqueProperties(options.Metadata, saga);

            VersionedSagaEntity sagaEntity;
            if (data.TryGetValue(saga.Id.ToString(), out sagaEntity))
            {
                sagaEntity.ConcurrencyCheck(saga, version);
            }

            data.AddOrUpdate(saga.Id.ToString(), id => new VersionedSagaEntity { SagaEntity = DeepClone(saga) }, (id, original) => new VersionedSagaEntity { SagaEntity = DeepClone(saga), VersionCache = original.VersionCache });

            Interlocked.Increment(ref version);
        }

        public void Update(IContainSagaData saga, SagaPersistenceOptions options)
        {
            Save(saga, options);
        }

        void ValidateUniqueProperties(SagaMetadata sagaMetaData, IContainSagaData saga)
        {
            var sagaType = saga.GetType();
            var existingSagas = (from s in data
                where s.Value.SagaEntity.GetType() == sagaType && (s.Key != saga.Id.ToString())
                select s.Value)
                .ToList();
            foreach (var correlationProperty in sagaMetaData.CorrelationProperties)
            {
                if (correlationProperty.Name == null)
                {
                    continue;
                }

                var uniqueProperty = sagaType.GetProperty(correlationProperty.Name);
                if (!uniqueProperty.CanRead)
                {
                    continue;
                }
                var incomingSagaPropertyValue = uniqueProperty.GetValue(saga, null);
                if (incomingSagaPropertyValue == null)
                {
                    var message = string.Format("Cannot store saga with id '{0}' since the unique property '{1}' has a null value.", saga.Id, uniqueProperty.Name);
                    throw new InvalidOperationException(message);
                }

                foreach (var storedSaga in existingSagas)
                {
                    var storedSagaPropertyValue = uniqueProperty.GetValue(storedSaga.SagaEntity, null);
                    if (Equals(incomingSagaPropertyValue, storedSagaPropertyValue))
                    {
                        var message = string.Format("Cannot store a saga. The saga with id '{0}' already has property '{1}'.", storedSaga.SagaEntity.Id, uniqueProperty.Name);
                        throw new InvalidOperationException(message);
                    }
                }
            }
        }

        IContainSagaData DeepClone(IContainSagaData source)
        {
            var json = serializer.SerializeObject(source);
            return (IContainSagaData)serializer.DeserializeObject(json, source.GetType());
        }

        class VersionedSagaEntity
        {
            public IContainSagaData SagaEntity;

            public WeakKeyDictionary<IContainSagaData, int> VersionCache = new WeakKeyDictionary<IContainSagaData, int>();

            public void RecordRead(IContainSagaData sagaEntity, int currentVersion)
            {
                VersionCache[sagaEntity] = currentVersion;
            }

            public void ConcurrencyCheck(IContainSagaData sagaEntity, int currentVersion)
            {
                int v;
                if (!VersionCache.TryGetValue(sagaEntity, out v))
                    throw new Exception(string.Format("InMemorySagaPersister in an inconsistent state: entity Id[{0}] not read.", sagaEntity.Id));

                if (v != currentVersion)
                    throw new Exception(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", sagaEntity.Id));
            }
        }
    }
}
