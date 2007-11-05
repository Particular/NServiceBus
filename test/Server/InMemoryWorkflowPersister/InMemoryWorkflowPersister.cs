using System;
using System.Collections.Generic;
using System.Text;
using NServiceBus.Workflow;

namespace InMemoryWorkflowPersister
{
    public class InMemoryWorkflowPersister : IWorkflowPersister
    {
        #region IWorkflowPersister Members

        public void Save(IWorkflowEntity wf)
        {
            this.lookup[wf.Id] = wf;
        }

        public IWorkflowEntity Get(Guid workflowId)
        {
            IWorkflowEntity result = null;
            this.lookup.TryGetValue(workflowId, out result);

            return result;
        }

        public void Complete(IWorkflowEntity wf)
        {
            this.lookup.Remove(wf.Id);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        private IDictionary<Guid, IWorkflowEntity> lookup = new Dictionary<Guid, IWorkflowEntity>();
    }
}
