using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
	/// <summary>
	/// A marker interface that also defines the properties of a
	/// message involved in an NServiceBus workflow.
	/// </summary>
    public interface IWorkflowMessage : IMessage
    {
		/// <summary>
		/// Gets/sets the Id of the workflow the message is related to.
		/// </summary>
        Guid WorkflowId { get; set; }
    }

	/// <summary>
	/// Indicates that the object starts a workflow.
	/// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StartsWorkflowAttribute : Attribute
    {
    }
}
