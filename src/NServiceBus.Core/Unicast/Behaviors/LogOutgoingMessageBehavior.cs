namespace NServiceBus
{
    using System;
    using System.Linq;
    using Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;
    using Pipeline.Contexts;

    class LogOutgoingMessageBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            var options = context.DeliveryMessageOptions as SendMessageOptions;

            if (options != null && log.IsDebugEnabled && context.OutgoingLogicalMessage != null)
            {
                var destination = options.Destination;

                log.DebugFormat("Sending message '{0}' with id '{1}' to destination '{2}'.\n" +
                                "ToString() of the message yields: {3}\n" +
                                "Message headers:\n{4}",
                                context.IsControlMessage() ?  "[Control message]" : context.OutgoingLogicalMessage.MessageType.AssemblyQualifiedName,
                    context.MessageId,
                    destination,
                    context.IsControlMessage() ? ((ControlMessage)context.OutgoingLogicalMessage).Purpose : context.OutgoingLogicalMessage.Instance,
                    string.Join(", ", context.Headers.Select(h => h.Key + ":" + h.Value).ToArray()));
            }

            next();

        }

        static ILog log = LogManager.GetLogger("LogOutgoingMessage");
    }
}