using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.InMemory
{
    /// <summary>
    /// In memory implementation of ISagaPersister for quick development.
    /// </summary>
    public class InMemorySagaPersister : ISagaPersister
    {
        void ISagaPersister.Complete(ISagaEntity saga)
        {
            data.Remove(saga.Id);
        }

        T ISagaPersister.Get<T>(string property, object value)
        {
            foreach(var entity in data.Values)
            {
                var prop = entity.GetType().GetProperty(property);
                if (prop != null)
                    if (prop.GetValue(entity, null).Equals(value))
                        return (T)entity;
            }

            return default(T);
        }

        T ISagaPersister.Get<T>(Guid sagaId)
        {
            ISagaEntity result;
            data.TryGetValue(sagaId, out result);

            return (T)result;
        }

        void ISagaPersister.Save(ISagaEntity saga)
        {
            data[saga.Id] = saga;
        }

        void ISagaPersister.Update(ISagaEntity saga)
        {
            ((ISagaPersister)this).Save(saga);
        }

        private readonly IDictionary<Guid, ISagaEntity> data = new Dictionary<Guid, ISagaEntity>();
    }
}