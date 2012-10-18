using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NServiceBus.Saga;
using NServiceBus.Serialization;

namespace NServiceBus.SagaPersisters.InMemory
{
    /// <summary>
    /// In memory implementation of ISagaPersister for quick development.
    /// </summary>
    public class InMemorySagaPersister : ISagaPersister
    {
        public static Func<IMessageSerializer> ConfigureSerializer = () => { return Configure.Instance.Builder.Build<IMessageSerializer>(); };

        void ISagaPersister.Complete(ISagaEntity saga)
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
                            return (T)entity.SagaEntity;
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
                    return (T)DeepClone(result.SagaEntity);
            }
            return default(T);
        }

        void ISagaPersister.Save(ISagaEntity saga)
        {
            lock (syncRoot)
            {
                ValidateUniqueProperties(saga);

                int version = 0;
                if (data.ContainsKey(saga.Id))
                {
                    var vse = data[saga.Id];
                    if (vse.SavedByThreadId != 0)
                        throw new InvalidOperationException(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved by Worker[{1}]", saga.Id, vse.SavedByThreadId));
                    version = Thread.CurrentThread.ManagedThreadId;
                }

                data[saga.Id] = new VersionedSagaEntity { SagaEntity = DeepClone(saga), SavedByThreadId = version };
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

        private readonly IDictionary<Guid, VersionedSagaEntity> data = new Dictionary<Guid, VersionedSagaEntity>();
        private readonly object syncRoot = new object();
        private readonly IMessageSerializer serializer = ConfigureSerializer();

        private ISagaEntity DeepClone(ISagaEntity source)
        {
            var stream = new MemoryStream();
            serializer.Serialize(new[] { source }, stream);
            stream.Position = 0;
            var obj = serializer.Deserialize(stream);
            return obj[0] as ISagaEntity;
        }

        private class VersionedSagaEntity
        {
            public ISagaEntity SagaEntity;
            public int SavedByThreadId;
        }
    }
}
