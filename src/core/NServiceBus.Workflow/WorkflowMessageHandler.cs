using System;
using System.Collections.Generic;
using System.Text;
using ObjectBuilder;
using System.Reflection;
using Common.Logging;

namespace NServiceBus.Workflow
{
	/// <summary>
	/// A message handler that supports long running workflow.
	/// </summary>
    public class WorkflowMessageHandler : BaseMessageHandler<IMessage>
    {
		/// <summary>
		/// Handles a message.
		/// </summary>
		/// <param name="message">The message to handle.</param>
		/// <remarks>
		/// If the message received is has the <see cref="StartsWorkflowAttribute"/> defined then a new
		/// workflow instance will be created and will be saved using the <see cref="IWorkflowPersister"/>
		/// implementation provided in the configuration.  Any other message implementing 
		/// <see cref="IWorkflowMessage"/> will cause the existing workflow instance with which it is
		/// associated to continue.</remarks>
        public override void Handle(IMessage message)
        {
            if (message is IWorkflowMessage)
            {
                this.HandleContinuing(message as IWorkflowMessage);
                return;
            }

            if (message.GetType().GetCustomAttributes(typeof(StartsWorkflowAttribute), true).Length > 0)
                this.HandleStartingWorkflow(message);
        }

		/// <summary>
		/// Creates and persists the appropriate workflow entity for the provided message.
		/// </summary>
		/// <param name="message">The message to start a workflow for.</param>
        private void HandleStartingWorkflow(IMessage message)
        {
            Type specificWorkflowType = this.GetWorkflowTypeForMessage(message);

            IWorkflowEntity wf = this.builder.Build(specificWorkflowType) as IWorkflowEntity;

            using (IWorkflowPersister persister = this.builder.Build<IWorkflowPersister>())
            {
                this.HandleMessageOnWorkflow(wf, message);

                if (wf.Completed)
                    persister.Complete(wf);
                else
                    persister.Save(wf);
            }

            LogIfWorkflowCompleted(wf);
        }

		/// <summary>
		/// Gets the workflow entity associated with the message from the persistence
		/// store and handles it.
		/// </summary>
		/// <param name="message">The message to handle.</param>
        private void HandleContinuing(IWorkflowMessage message)
        {
            TimeoutMessage tm = message as TimeoutMessage;

            IWorkflowEntity wf = null;
            using (IWorkflowPersister persister = this.builder.Build<IWorkflowPersister>())
            {
                wf = persister.Get(message.WorkflowId);
                
                if (wf == null)
                    return;

                if (tm != null && tm.HasNotExpired())
                {
                    this.Bus.HandleCurrentMessageLater();
                    return;
                }

                if (tm != null)
                    wf.Timeout(tm.State);
                else
                    this.HandleMessageOnWorkflow(wf, message);

                if (wf.Completed)
                    persister.Complete(wf);
                else
                    persister.Save(wf);
            }

            LogIfWorkflowCompleted(wf);
        }

		/// <summary>
		/// Logs that a workflow completed.
		/// </summary>
		/// <param name="wf">The workflow that completed.</param>
        private static void LogIfWorkflowCompleted(IWorkflowEntity wf)
        {
            if (wf.Completed)
                logger.Debug(wf.GetType().Name.ToString() + " " + wf.Id.ToString() + " has completed.");
        }

		/// <summary>
		/// Gets the type of workflow associated with the specified message.
		/// </summary>
		/// <param name="message">The message to get the workflow type for.</param>
		/// <returns>The <see cref="IWorkflow<>"/> type associated with the message.</returns>
        private Type GetWorkflowTypeForMessage(IMessage message)
        {
            return typeof(IWorkflow<>).MakeGenericType(message.GetType());
        }

		/// <summary>
		/// Invokes the message handler on the workflow for the message.
		/// </summary>
		/// <param name="workflow">The workflow to call the message handler for.</param>
		/// <param name="message">The message to pass to the handler.</param>
        private void HandleMessageOnWorkflow(object workflow, IMessage message)
        {
            MethodInfo method = workflow.GetType().GetMethod("Handle", new Type[] { message.GetType() });

            if (method != null)
                method.Invoke(workflow, new object[] { message });
        }

        #region config info

        private IBuilder builder;

		/// <summary>
		/// Gets/sets an <see cref="IBuilder"/> that will be used for resolving
		/// the <see cref="IWorkflowPersister"/> implementation to be used for
		/// workflow persistence.
		/// </summary>
        public IBuilder Builder
        {
            get { return builder; }
            set { builder = value; }
        }

        #endregion

        private static ILog logger = LogManager.GetLogger(typeof(WorkflowMessageHandler));
    }
}
