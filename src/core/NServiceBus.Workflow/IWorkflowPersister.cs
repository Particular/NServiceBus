using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
    /// <summary>
    /// Use per-instance (single-call) semantics for instantiation rather than singleton.
    /// </summary>
    public interface IWorkflowPersister : IDisposable
    {
        void Save(IWorkflowEntity wf);
        IWorkflowEntity Get(Guid workflowId);
        void Complete(IWorkflowEntity wf);
    }
}
