namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MultiMessageBehavior : IBehavior<SendLogicalMessagesContext>
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "5.0")]
        public void Invoke(SendLogicalMessagesContext context, Action next)
        {
            foreach (var logicalMessage in context.LogicalMessages)
            {
                PipelineExecutor.InvokeSendPipeline(context.SendOptions, logicalMessage);
            }
            next();
        }
    }
}