using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
    public class Reminder
    {
        public void ExpireIn(TimeSpan span, IWorkflowEntity workflow, object state)
        {
            TimeoutMessage msg = new TimeoutMessage();
            msg.Expires = DateTime.Now + span;
            msg.WorkflowId = workflow.Id;

            this.bus.HandleMessagesLater(msg);
        }

        #region config info

        private IBus bus;
        public IBus Bus
        {
            get { return bus; }
            set { bus = value; }
        }

        #endregion
    }
}
