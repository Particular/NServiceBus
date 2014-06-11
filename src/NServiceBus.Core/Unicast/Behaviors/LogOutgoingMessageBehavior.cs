namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Linq;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;

    class LogOutgoingMessageBehavior : IBehavior<OutgoingContext>
    {
        public void Invoke(OutgoingContext context, Action next)
        {
            var options = context.DeliveryOptions as SendOptions;
            if (options != null)
            {
                var destination = options.Destination.ToString();

                log.DebugFormat("Sending message '{0}' with id '{1}' to destination '{2}'.\n" +
                                "ToString() of the message yields: {3}\n" +
                                "Message headers:\n{4}",
                    context.OutgoingLogicalMessage.MessageType.AssemblyQualifiedName,
                    context.OutgoingMessage.Id,
                    destination,
                    context.OutgoingLogicalMessage.Instance,
                    string.Join(", ", context.OutgoingLogicalMessage.Headers.Select(h => h.Key + ":" + h.Value).ToArray()
                        ));
            }

            next();
        }

        static ILog log = LogManager.GetLogger("LogOutgoingMessage");
    }
}