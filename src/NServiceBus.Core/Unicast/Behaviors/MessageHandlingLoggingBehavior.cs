namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Reflection;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    
    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MessageHandlingLoggingBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            var msg = context.PhysicalMessage;
            log.DebugFormat("Received message with ID {0} from sender {1}", msg.Id, msg.ReplyToAddress);

            next();

            log.Debug("Finished handling message.");
        }
    }
}