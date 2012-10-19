using System;
using System.Linq;
using System.Collections.Generic;
using NServiceBus.Saga;
using System.Threading;
using NServiceBus.Serialization;
using NServiceBus.Serializers.Json;
using System.IO;
using System.Reflection;

namespace NServiceBus.SagaPersisters.InMemory
{
    /// <summary>
    /// In memory implementation of ISagaPersister for quick development.
    /// </summary>
    public class InMemorySagaPersister : ISagaPersister
    {
        void ISagaPersister.Complete(ISagaEntity saga)
        {
            lock(syncRoot)
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
            lock(syncRoot)
            {
                VersionedSagaEntity result;
                data.TryGetValue(sagaId, out result);
                if((result != null) && (result.SagaEntity is T))
                {
                    result.ReadByThreadId.Add(Thread.CurrentThread.ManagedThreadId);
                    return (T)DeepClone(result.SagaEntity);
                }
            }
            return default(T);
        }

        void ISagaPersister.Save(ISagaEntity saga)
        {
            lock(syncRoot)
            {
                ValidateUniqueProperties(saga);

                if (data.ContainsKey(saga.Id))
                    data[saga.Id].ConcurrencyCheck();

                data[saga.Id] = new VersionedSagaEntity { SagaEntity = DeepClone(saga) };
            }
        }
        
        void ISagaPersister.Update(ISagaEntity saga)
        {
            ((ISagaPersister)this).Save(saga);
        }
        
        private void ValidateUniqueProperties(ISagaEntity saga)
        {
            var uniqueProperties = UniqueAttribute.GetUniqueProperties(saga.GetType());
            if (!uniqueProperties.Any()) return;

            var sagasFromSameType = from s in data
                                    where
                                        ((s.Value.SagaEntity as ISagaEntity).GetType() == saga.GetType() && (s.Key != saga.Id))
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
                                               storedSaga.SagaEntity.Id, uniqueProperty.ToString(), storedSagaPropertyValue));
                    }
                }
        }

        private class VersionedSagaEntity
        {
            public ISagaEntity SagaEntity;

	        public void ConcurrencyCheck()
	        {
                if (!ReadByThreadId.Contains(Thread.CurrentThread.ManagedThreadId))
                    throw new InvalidOperationException(
	                    string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved by [Worker.{1}]",
	                    SagaEntity.Id, SavedByThreadId));
	        }

	        readonly public IList<int> ReadByThreadId = new List<int>();
	        readonly public int SavedByThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private ISagaEntity DeepClone(ISagaEntity source)
        {
            var data = serializer.SerializeObject(source);

            //dynamic invoke used as JsonMessageSerializer doesn't de-serialize to an interface
            return typeof(JsonMessageSerializer)
	            .GetMethod("DeserializeObject")
	            .MakeGenericMethod(source.GetType())
	            .Invoke(serializer, new[] { data }) as ISagaEntity;
        }

        private readonly JsonMessageSerializer serializer = new JsonMessageSerializer(null);
        private readonly IDictionary<Guid, VersionedSagaEntity> data = new Dictionary<Guid, VersionedSagaEntity>();
        private readonly object syncRoot = new object();
    }
}
