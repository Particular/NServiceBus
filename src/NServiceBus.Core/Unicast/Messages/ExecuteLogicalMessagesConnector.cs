namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using NServiceBus.Unicast.Transport;
    using Pipeline;
    using Pipeline.Contexts;

    class ExecuteLogicalMessagesConnector : StageConnector<LogicalMessagesProcessingStageBehavior.Context, LogicalMessageProcessingStageBehavior.Context>
    {
        public override void Invoke(LogicalMessagesProcessingStageBehavior.Context context, Action<LogicalMessageProcessingStageBehavior.Context> next)
        {
            var logicalMessages = context.LogicalMessages;

            foreach (var message in logicalMessages)
            {
                next(new LogicalMessageProcessingStageBehavior.Context(message, context));
            }

            if (!context.PhysicalMessage.IsControlMessage())
            {
                if (!logicalMessages.Any())
                {
                    log.Warn("Received an empty message - ignoring.");
                }
            }
        }

        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}