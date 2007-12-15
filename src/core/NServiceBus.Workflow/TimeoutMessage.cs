using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
	/// <summary>
	/// A message to signal a workflow that a reminder was set.
	/// </summary>
    [Serializable]
    [Recoverable]
    public class TimeoutMessage : IWorkflowMessage
    {
        private DateTime expires;

		/// <summary>
		/// Gets/sets the date and time at which the timeout message is due to expire.
		/// </summary>
        public DateTime Expires
        {
            get { return expires; }
            set { expires = value; }
        }

        private Guid workflowId;

		/// <summary>
		/// Gets/sets the Id of the workflow the TimeoutMessage is connected to.
		/// </summary>
        public Guid WorkflowId
        {
            get { return workflowId; }
            set { workflowId = value; }
        }

        private object state;

        /// <summary>
        /// Contains the object passed as the state parameter
        /// to the ExpireIn method of <see cref="Reminder"/>.
        /// </summary>
        public object State
        {
            get { return state; }
            set { state = value; }
        }

		/// <summary>
		/// Gets whether or not the TimeoutMessage has expired.
		/// </summary>
		/// <returns>true if the message has expired, otherwise false.</returns>
        public bool HasNotExpired()
        {
            return DateTime.Now < this.expires;
        }
    }
}
