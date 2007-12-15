using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
    /// <summary>
    /// Defines the basic functionality of a persister for storing 
	/// and retrieving a workflow.
    /// </summary>
	/// <remarks>
	/// Use per-instance (single-call) semantics for instantiation rather than singleton.
	/// </remarks>
    public interface IWorkflowPersister : IDisposable
    {
		/// <summary>
		/// Saves the workflow entity to the persistence store.
		/// </summary>
		/// <param name="wf">The workflow entity to save.</param>
        void Save(IWorkflowEntity wf);

		/// <summary>
		/// Gets a workflow entity from the persistence store by its Id.
		/// </summary>
		/// <param name="workflowId">The Id of the workflow entity to get.</param>
		/// <returns></returns>
        IWorkflowEntity Get(Guid workflowId);

		/// <summary>
		/// Sets a workflow as completed and removes it from the active workflow list
		/// in the persistence store.
		/// </summary>
		/// <param name="wf">The workflow to complete.</param>
        void Complete(IWorkflowEntity wf);
    }
}
