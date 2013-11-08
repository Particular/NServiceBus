namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// This one can be removed in v5.0 
    /// </summary>
    internal class MultiMessageBehavior:IBehavior<SendLogicalMessagesContext>
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