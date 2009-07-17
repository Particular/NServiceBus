using System;
using System.Collections.Generic;
using NServiceBus.Saga;
namespace NServiceBus.Host.Internal
{
    public class InMemorySagaPersister : ISagaPersister
    {
        public void Complete(ISagaEntity saga)
        {
            data.Remove(saga.Id);
        }

        public T Get<T>(string property, object value) where T : ISagaEntity
        {
            foreach(var entity in data.Values)
            {
                var prop = entity.GetType().GetProperty(property);
                if (prop != null)
                    if (prop.GetValue(entity, null) == value)
                        return (T)entity;
            }

            return default(T);
        }

        public T Get<T>(Guid sagaId) where T : ISagaEntity
        {
            ISagaEntity result;
            data.TryGetValue(sagaId, out result);

            return (T)result;
        }

        public void Save(ISagaEntity saga)
        {
            data[saga.Id] = saga;
        }

        public void Update(ISagaEntity saga)
        {
            Save(saga);
        }

        private readonly IDictionary<Guid, ISagaEntity> data = new Dictionary<Guid, ISagaEntity>();
    }
}
