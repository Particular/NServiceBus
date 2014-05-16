namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Reflection;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    
    class MessageHandlingLoggingBehavior : IBehavior<IncomingContext>
    {
        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Invoke(IncomingContext context, Action next)
        {
            var msg = context.PhysicalMessage;
            log.DebugFormat("Received message with ID {0} from sender {1}", msg.Id, msg.ReplyToAddress);

            next();

            log.Debug("Finished handling message.");
        }
    }
}