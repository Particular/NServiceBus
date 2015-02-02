namespace NServiceBus.InMemory.SagaPersister
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using NServiceBus.Sagas;
    using Saga;
    using Serializers.Json;

    /// <summary>
    /// In memory implementation of ISagaPersister for quick development.
    /// </summary>
    class InMemorySagaPersister : ISagaPersister
    {
        readonly SagaMetaModel sagaModel;

        public InMemorySagaPersister(SagaMetaModel sagaModel)
        {
            this.sagaModel = sagaModel;
        }

    
        public void Complete(IContainSagaData saga)
        {
            VersionedSagaEntity value;
            data.TryRemove(saga.Id, out value);
        }

        public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
        {
            var values = data.Values.Where(x => x.SagaEntity is TSagaData);
            foreach (var entity in values)
            {
                var prop = entity.SagaEntity.GetType().GetProperty(propertyName);
                if (prop == null)
                {
                    continue;
                }
                if (!prop.GetValue(entity.SagaEntity, null).Equals(propertyValue))
                {
                    continue;
                }
                entity.RecordRead();
                return (TSagaData)DeepClone(entity.SagaEntity);
            }
            return default(TSagaData);
        }

        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            VersionedSagaEntity result;
            if (data.TryGetValue(sagaId, out result) && (result != null) && (result.SagaEntity is TSagaData))
            {
                result.RecordRead();
                return (TSagaData)DeepClone(result.SagaEntity);
            }
            return default(TSagaData);
        }

        public void Save(IContainSagaData saga)
        {
            ValidateUniqueProperties(saga);

            VersionedSagaEntity sagaEntity;
            if (data.TryGetValue(saga.Id, out sagaEntity))
            {
                sagaEntity.ConcurrencyCheck();
            }

            data.AddOrUpdate(saga.Id, id => new VersionedSagaEntity { SagaEntity = DeepClone(saga) }, (id, original) => new VersionedSagaEntity { SagaEntity = DeepClone(saga) });
        }

        public void Update(IContainSagaData saga)
        {
            Save(saga);
        }

        void ValidateUniqueProperties(IContainSagaData saga)
        {
            var sagaMetaData = sagaModel.FindByEntityName(saga.GetType().FullName);


            if (!sagaMetaData.CorrelationProperties.Any()) return;

            var sagasFromSameType = from s in data
                                    where
                                        (s.Value.SagaEntity.GetType() == saga.GetType() && (s.Key != saga.Id))
                                    select s.Value;

            foreach (var storedSaga in sagasFromSameType)
            {
                foreach (var correlationProperty in sagaMetaData.CorrelationProperties)
                {
                    var uniqueProperty = saga.GetType().GetProperty(correlationProperty.Name);
                    if (!uniqueProperty.CanRead)
                    {
                        continue;
                    }
                    var inComingSagaPropertyValue = uniqueProperty.GetValue(saga, null);
                    var storedSagaPropertyValue = uniqueProperty.GetValue(storedSaga.SagaEntity, null);
                    if (inComingSagaPropertyValue.Equals(storedSagaPropertyValue))
                    {
                        var message = string.Format("Cannot store a saga. The saga with id '{0}' already has property '{1}' with value '{2}'.",storedSaga.SagaEntity.Id, uniqueProperty, storedSagaPropertyValue);
                        throw new InvalidOperationException(message);
                    }
                }
            }
              
        }

        public class VersionedSagaEntity
        {
            public IContainSagaData SagaEntity;

            readonly ConcurrentDictionary<int, byte> readByThreadId = new ConcurrentDictionary<int, byte>();

            public void RecordRead()
            {
                readByThreadId.AddOrUpdate(Thread.CurrentThread.ManagedThreadId, 0, (id, value) => 0);
            }

            public void ConcurrencyCheck()
            {
                var currentThreadId = Thread.CurrentThread.ManagedThreadId;
                if (!readByThreadId.ContainsKey(currentThreadId))
                    throw new Exception(
                        string.Format(
                            "InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved by [Worker.{1}]",
                            SagaEntity.Id, currentThreadId));
            }
        }

        IContainSagaData DeepClone(IContainSagaData source)
        {
            var json = serializer.SerializeObject(source);

            return (IContainSagaData)serializer.DeserializeObject(json, source.GetType());
        }

        public IDictionary<Guid, VersionedSagaEntity> CurrentSagaEntities
        {
            get
            {
                return data;
            }
        }

        JsonMessageSerializer serializer = new JsonMessageSerializer(null);
        ConcurrentDictionary<Guid, VersionedSagaEntity> data = new ConcurrentDictionary<Guid, VersionedSagaEntity>();
    }
}
