namespace NServiceBus.Persistence.InMemory.SagaPersister
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
    public class InMemorySagaPersister : ISagaPersister
    {
        void ISagaPersister.Complete(IContainSagaData saga)
        {
            lock (syncRoot)
            {
                data.Remove(saga.Id);
            }
        }

        T ISagaPersister.Get<T>(string property, object value)
        {
            lock (syncRoot)
            {
                var values = data.Values.Where(x => x.SagaEntity is T);
                foreach (var entity in values)
                {
                    var prop = entity.SagaEntity.GetType().GetProperty(property);
                    if (prop != null)
                        if (prop.GetValue(entity.SagaEntity, null).Equals(value))
                        {
                            entity.ReadByThreadId.Add(Thread.CurrentThread.ManagedThreadId);
                            return (T)DeepClone(entity.SagaEntity);
                        }
                }
            }
            return default(T);
        }

        T ISagaPersister.Get<T>(Guid sagaId)
        {
            lock (syncRoot)
            {
                VersionedSagaEntity result;
                data.TryGetValue(sagaId, out result);
                if ((result != null) && (result.SagaEntity is T))
                {
                    result.ReadByThreadId.Add(Thread.CurrentThread.ManagedThreadId);
                    return (T)DeepClone(result.SagaEntity);
                }
            }
            return default(T);
        }

        void ISagaPersister.Save(IContainSagaData saga)
        {
            lock (syncRoot)
            {
                ValidateUniqueProperties(saga);

                if (data.ContainsKey(saga.Id))
                {
                    data[saga.Id].ConcurrencyCheck();
                }

                data[saga.Id] = new VersionedSagaEntity { SagaEntity = DeepClone(saga) };
            }
        }

        void ISagaPersister.Update(IContainSagaData saga)
        {
            ((ISagaPersister)this).Save(saga);
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

        private class VersionedSagaEntity
        {
            public IContainSagaData SagaEntity;

            public void ConcurrencyCheck()
            {
                if (!ReadByThreadId.Contains(Thread.CurrentThread.ManagedThreadId))
                    throw new ConcurrencyException(
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

        private readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);
        private readonly IDictionary<Guid, VersionedSagaEntity> data = new Dictionary<Guid, VersionedSagaEntity>();
        private readonly object syncRoot = new object();
    }
}
