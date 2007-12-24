using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace InMemorySagaPersister
{
    public class InMemorySagaPersister : ISagaPersister
    {
        #region ISagaPersister Members

        public void Save(ISagaEntity saga)
        {
            this.lookup[saga.Id] = saga;
        }

        public void Update(ISagaEntity saga)
        {
            this.lookup[saga.Id] = saga;
        }

        public ISagaEntity Get(Guid sagaId)
        {
            ISagaEntity result;
            this.lookup.TryGetValue(sagaId, out result);

            return result;
        }

        public void Complete(ISagaEntity saga)
        {
            this.lookup.Remove(saga.Id);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        private readonly IDictionary<Guid, ISagaEntity> lookup = new Dictionary<Guid, ISagaEntity>();
    }
}
