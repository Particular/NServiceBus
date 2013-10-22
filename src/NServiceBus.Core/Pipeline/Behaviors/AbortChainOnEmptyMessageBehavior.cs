namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Reflection;
    using Logging;
    using Pipeline;
    using Unicast.Transport;

    class AbortChainOnEmptyMessageBehavior : IBehavior
    {
        static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Invoke(BehaviorContext context, Action next)
        {
            var transportMessage = context.TransportMessage;

            if (!transportMessage.IsControlMessage() && LogicalMessageCount(context) == 0)
            {
                context.Trace("Ignoring empty message with ID {0}", transportMessage.Id);
                log.Warn("Received an empty message - ignoring.");
                return;
            }

            next();
        }

        static int LogicalMessageCount(BehaviorContext context)
        {
            return context.Messages == null
                       ? 0
                       : context.Messages.Length;
        }
    }
}