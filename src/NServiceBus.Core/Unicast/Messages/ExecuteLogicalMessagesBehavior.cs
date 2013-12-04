namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;

    class ExecuteLogicalMessagesBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public PipelineFactory PipelineFactory { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            if (context.MessageHandlingDisabled)
            {
                return;
            }


            var logicalMessages = context.LogicalMessages;
            foreach (var message in logicalMessages)
            {
                PipelineFactory.InvokeLogicalMessagePipeline(message);
            }

            if (!logicalMessages.Any())
            {
                log.Warn("Received an empty message - ignoring.");
            }

            next();
        }


        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}