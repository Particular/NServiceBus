namespace NServiceBus
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;

    class LogOutgoingMessageBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            var options = context.DeliveryMessageOptions as SendMessageOptions;

            if (options != null && log.IsDebugEnabled && context.MessageInstance != null)
            {
                var destination = options.Destination;

                log.DebugFormat("Sending message '{0}' with id '{1}' to destination '{2}'.\n" +
                                "ToString() of the message yields: {3}",
                                context.MessageType.AssemblyQualifiedName,
                    context.MessageId,
                    destination,
                    context.MessageInstance);
            }

            next();

        }

        static ILog log = LogManager.GetLogger("LogOutgoingMessage");
    }
}