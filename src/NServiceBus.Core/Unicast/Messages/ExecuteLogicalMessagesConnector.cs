namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Logging;
    using NServiceBus.Unicast.Transport;
    using Pipeline;
    using Pipeline.Contexts;

    class ExecuteLogicalMessagesConnector : StageConnector<LogicalMessagesProcessingStageBehavior.Context, LogicalMessageProcessingStageBehavior.Context>
    {
        public override async Task Invoke(LogicalMessagesProcessingStageBehavior.Context context, Func<LogicalMessageProcessingStageBehavior.Context, Task> next)
        {
            var logicalMessages = context.LogicalMessages;

            foreach (var message in logicalMessages)
            {
                await next(new LogicalMessageProcessingStageBehavior.Context(message, context.GetPhysicalMessage().Headers, message.MessageType, context));
            }

            if (!TransportMessageExtensions.IsControlMessage(context.GetPhysicalMessage().Headers))
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