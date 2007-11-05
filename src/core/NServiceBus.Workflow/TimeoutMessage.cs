using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
    [Serializable]
    [Recoverable]
    public class TimeoutMessage : IWorkflowMessage
    {
        private DateTime expires;
        public DateTime Expires
        {
            get { return expires; }
            set { expires = value; }
        }

        private Guid workflowId;
        public Guid WorkflowId
        {
            get { return workflowId; }
            set { workflowId = value; }
        }

        private object state;
        public object State
        {
            get { return state; }
            set { state = value; }
        }

        public bool HasNotExpired()
        {
            return DateTime.Now < this.expires;
        }
    }
}
