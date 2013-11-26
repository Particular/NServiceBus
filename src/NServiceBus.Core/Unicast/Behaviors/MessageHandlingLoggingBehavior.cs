namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Reflection;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;

    /// <summary>
    /// Arguably not the most interesting behavior, but let's just handle logging like this too
    /// </summary>
    class MessageHandlingLoggingBehavior : IBehavior<ReceivePhysicalMessageContext>
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