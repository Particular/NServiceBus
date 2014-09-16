namespace NServiceBus.InMemory.SagaPersister
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Saga;
    using Serializers.Json;

    /// <summary>
    /// In memory implementation of ISagaPersister for quick development.
    /// </summary>
    class InMemorySagaPersister : ISagaPersister
    {
        public void Complete(IContainSagaData saga)
        {
            lock (syncRoot)
            {
                data.Remove(saga.Id);
            }
        }

        public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
        {
            lock (syncRoot)
            {
                var values = data.Values.Where(x => x.SagaEntity is TSagaData);
                foreach (var entity in values)
                {
                    var prop = entity.SagaEntity.GetType().GetProperty(propertyName);
                    if (prop != null)
                    {
                        if (prop.GetValue(entity.SagaEntity, null).Equals(propertyValue))
                        {
                            entity.ReadByThreadId.Add(Thread.CurrentThread.ManagedThreadId);
                            return (TSagaData)DeepClone(entity.SagaEntity);
                        }
                    }
                }
            }
            return default(TSagaData);
        }

        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            lock (syncRoot)
            {
                VersionedSagaEntity result;
                data.TryGetValue(sagaId, out result);
                if ((result != null) && (result.SagaEntity is TSagaData))
                {
                    result.ReadByThreadId.Add(Thread.CurrentThread.ManagedThreadId);
                    return (TSagaData)DeepClone(result.SagaEntity);
                }
            }
            return default(TSagaData);
        }

        public void Save(IContainSagaData saga)
        {
            lock (syncRoot)
            {
                ValidateUniqueProperties(saga);

                VersionedSagaEntity sagaEntity;
                if (data.TryGetValue(saga.Id, out sagaEntity))
                {
                    sagaEntity.ConcurrencyCheck();
                }

                data[saga.Id] = new VersionedSagaEntity { SagaEntity = DeepClone(saga) };
            }
        }

        public void Update(IContainSagaData saga)
        {
            Save(saga);
        }

        private void ValidateUniqueProperties(IContainSagaData saga)
        {
            var uniqueProperties = UniqueAttribute.GetUniqueProperties(saga.GetType());
            if (!uniqueProperties.Any()) return;

            var sagasFromSameType = from s in data
                                    where
                                        (s.Value.SagaEntity.GetType() == saga.GetType() && (s.Key != saga.Id))
                                    select s.Value;

            foreach (var storedSaga in sagasFromSameType)
                foreach (var uniqueProperty in uniqueProperties)
                {
                    if (uniqueProperty.CanRead)
                    {
                        var inComingSagaPropertyValue = uniqueProperty.GetValue(saga, null);
                        var storedSagaPropertyValue = uniqueProperty.GetValue(storedSaga.SagaEntity, null);
                        if (inComingSagaPropertyValue.Equals(storedSagaPropertyValue))
                            throw new
                                InvalidOperationException(
                                string.Format("Cannot store a saga. The saga with id '{0}' already has property '{1}' with value '{2}'.",
                                               storedSaga.SagaEntity.Id, uniqueProperty, storedSagaPropertyValue));
                    }
                }
        }

        public class VersionedSagaEntity
        {
            public IContainSagaData SagaEntity;

            public void ConcurrencyCheck()
            {
                if (!ReadByThreadId.Contains(Thread.CurrentThread.ManagedThreadId))
                    throw new Exception(
                        string.Format(
                            "InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved by [Worker.{1}]",
                            SagaEntity.Id, savedByThreadId));
            }

            public readonly IList<int> ReadByThreadId = new List<int>();

            private readonly int savedByThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private IContainSagaData DeepClone(IContainSagaData source)
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

        private readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);
        private readonly IDictionary<Guid, VersionedSagaEntity> data = new Dictionary<Guid, VersionedSagaEntity>();
        private readonly object syncRoot = new object();
    }
}
