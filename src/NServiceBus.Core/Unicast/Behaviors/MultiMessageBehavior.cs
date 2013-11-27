namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;



    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "5.0")]
    public class MultiMessageBehavior : IBehavior<SendLogicalMessagesContext>
    {
        PipelineFactory pipelineFactory;

        internal MultiMessageBehavior(PipelineFactory pipelineFactory)
        {
            this.pipelineFactory = pipelineFactory;
        }

        public void Invoke(SendLogicalMessagesContext context, Action next)
        {
            foreach (var logicalMessage in context.LogicalMessages)
            {
                pipelineFactory.InvokeSendPipeline(context.SendOptions, logicalMessage);
            }
            next();
        }
    }
}