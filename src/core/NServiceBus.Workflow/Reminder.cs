using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
	/// <summary>
	/// A reminder that will send a <see cref="TimeoutMessage"/> to a workflow 
	/// after a period of time.
	/// </summary>
    public class Reminder
    {
		/// <summary>
		/// Sets how long until the specified workflow should be sent
		/// a <see cref="TimeoutMessage"/>.
		/// </summary>
		/// <param name="span">The time period to wait.</param>
		/// <param name="workflow">The workflow to remind.</param>
		/// <param name="state">An object that will be returned to the workflow object when the Timespan passes.</param>
        public void ExpireIn(TimeSpan span, IWorkflowEntity workflow, object state)
        {
            TimeoutMessage msg = new TimeoutMessage();
            msg.Expires = DateTime.Now + span;
            msg.WorkflowId = workflow.Id;

            this.bus.HandleMessagesLater(msg);
        }

        #region config info

        private IBus bus;

		/// <summary>
		/// Gets/sets the bus the reminder will be sent to.
		/// </summary>
        public IBus Bus
        {
            get { return bus; }
            set { bus = value; }
        }

        #endregion
    }
}
