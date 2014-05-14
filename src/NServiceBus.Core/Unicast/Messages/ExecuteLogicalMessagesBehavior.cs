namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Transport;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ExecuteLogicalMessagesBehavior : IBehavior<IncomingContext>
    {
        public PipelineExecutor PipelineExecutor { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            var logicalMessages = context.LogicalMessages;

            foreach (var message in logicalMessages)
            {
                using (context.CreateSnapshotRegion())
                {
                    context.IncomingLogicalMessage = message;
                    next();
                }
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