namespace NServiceBus
{
    using System;
    using System.Linq;
    using Logging;
    using NServiceBus.Unicast;
    using Pipeline.Contexts;

    class LogOutgoingMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var options = context.DeliveryOptions as SendOptions;

            if (options != null && log.IsDebugEnabled && context.OutgoingLogicalMessage != null)
            {
                var destination = options.Destination.ToString();

                log.DebugFormat("Sending message '{0}' with id '{1}' to destination '{2}'.\n" +
                                "ToString() of the message yields: {3}\n" +
                                "Message headers:\n{4}",
                                context.OutgoingLogicalMessage.MessageType != null ? context.OutgoingLogicalMessage.MessageType.AssemblyQualifiedName : "unknown",
                    context.OutgoingMessage.Id,
                    destination,
                    context.OutgoingLogicalMessage.Instance,
                    string.Join(", ", context.OutgoingLogicalMessage.Headers.Select(h => h.Key + ":" + h.Value).ToArray()));
            }

            next();

        }

        static ILog log = LogManager.GetLogger("LogOutgoingMessage");
    }
}