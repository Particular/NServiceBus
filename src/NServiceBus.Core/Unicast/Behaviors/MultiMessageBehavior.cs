namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "5.0")]
    public class MultiMessageBehavior : IBehavior<SendLogicalMessagesContext>
    {
        public PipelineFactory PipelineFactory { get; set; }

        public void Invoke(SendLogicalMessagesContext context, Action next)
        {
            foreach (var logicalMessage in context.LogicalMessages)
            {
                PipelineFactory.InvokeSendPipeline(context.SendOptions,logicalMessage);
            }
            next();
        }
    }
}