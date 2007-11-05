using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
    public interface IWorkflow<T> : IWorkflowEntity where T : IMessage
    {
        void Handle(T message);
    }

    public interface IWorkflowEntity
    {
        /// <summary>
        /// The reason Guid is used for workflow Id is that messages containing this Id need
        /// to be sent by the workflow even before it is persisted.
        /// </summary>
        Guid Id { get; }
        bool Completed { get; }

        void Timeout(object state);
    }
}
