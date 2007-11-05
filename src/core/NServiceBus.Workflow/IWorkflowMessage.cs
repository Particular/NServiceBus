using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus.Workflow
{
    public interface IWorkflowMessage : IMessage
    {
        Guid WorkflowId { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class StartsWorkflowAttribute : Attribute
    {
    }
}
