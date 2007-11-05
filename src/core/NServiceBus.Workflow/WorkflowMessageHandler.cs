using System;
using System.Collections.Generic;
using System.Text;
using ObjectBuilder;
using System.Reflection;
using Common.Logging;

namespace NServiceBus.Workflow
{
    public class WorkflowMessageHandler : BaseMessageHandler<IMessage>
    {
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

        private static void LogIfWorkflowCompleted(IWorkflowEntity wf)
        {
            if (wf.Completed)
                logger.Debug(wf.GetType().Name.ToString() + " " + wf.Id.ToString() + " has completed.");
        }

        private Type GetWorkflowTypeForMessage(IMessage message)
        {
            return typeof(IWorkflow<>).MakeGenericType(message.GetType());
        }

        private void HandleMessageOnWorkflow(object workflow, IMessage message)
        {
            MethodInfo method = workflow.GetType().GetMethod("Handle", new Type[] { message.GetType() });

            if (method != null)
                method.Invoke(workflow, new object[] { message });
        }

        #region config info

        private IBuilder builder;
        public IBuilder Builder
        {
            get { return builder; }
            set { builder = value; }
        }

        #endregion

        private static ILog logger = LogManager.GetLogger(typeof(WorkflowMessageHandler));
    }
}
