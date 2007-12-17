using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace InMemorySagaPersister
{
    public class InMemorySagaPersister : ISagaPersister
    {
        #region IWorkflowPersister Members

        public void Save(ISagaEntity wf)
        {
            this.lookup[wf.Id] = wf;
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
