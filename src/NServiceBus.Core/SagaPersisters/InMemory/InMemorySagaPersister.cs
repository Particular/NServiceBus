namespace NServiceBus.SagaPersisters.InMemory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Saga;

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
                var values = data.Values.Where(x => x is T);
                foreach (var entity in values)
                {
                    var prop = entity.GetType().GetProperty(property);
                    if (prop != null)
                        if (prop.GetValue(entity, null).Equals(value))
                            return (T) entity;
                }
            }
            return default(T);
        }

        T ISagaPersister.Get<T>(Guid sagaId)
        {
            lock(syncRoot)
            {
                ISagaEntity result;
                data.TryGetValue(sagaId, out result);
                if((result != null) && (result is T))
                    return (T)result;
            }
            return default(T);
        }

        void ISagaPersister.Save(ISagaEntity saga)
        {
            lock(syncRoot)
            {
                ValidateUniqueProperties(saga);
                data[saga.Id] = saga;
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
                                        ((s.Value as ISagaEntity).GetType() == saga.GetType() && (s.Key != saga.Id))
                                    select s.Value;

            foreach (var storedSaga in sagasFromSameType)
                foreach (var uniqueProperty in uniqueProperties)
                {
                    if (uniqueProperty.CanRead)
                    {
                        var inComingSagaPropertyValue = uniqueProperty.GetValue(saga, null);
                        var storedSagaPropertyValue = uniqueProperty.GetValue(storedSaga, null);
                        if (inComingSagaPropertyValue.Equals(storedSagaPropertyValue))
                            throw new
                                InvalidOperationException(
                                string.Format("Cannot store a saga. The saga with id '{0}' already has property '{1}' with value '{2}'.", storedSaga.Id, uniqueProperty.ToString(), storedSagaPropertyValue));
                    }
                }
        }



        
        private readonly IDictionary<Guid, ISagaEntity> data = new Dictionary<Guid, ISagaEntity>();
        private readonly object syncRoot = new object();
    }
}
