namespace NServiceBus
{
    using System;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Transport;

    class DisconnectMessageBehavior : IBehavior<IncomingContext>
    {
        public NoMessageBacklogNotifier Monitor { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            var transportMessage = context.PhysicalMessage;

            if (!transportMessage.IsControlMessage() && !IsDisconnectMessage(transportMessage))
            {
                Monitor.ResetTimer();
                next();
                return;
            }

            logger.Info("Received a notify for safe disconnect message, starting the timer.");
            Monitor.StartTimer(transportMessage.Headers);
        }

        bool IsDisconnectMessage(TransportMessage msg)
        {
            if (msg.Headers.ContainsKey(DisconnectHeader))
            {
                return true;
            }

            return false;
        }

        const string DisconnectHeader = "NServiceBus.DisconnectMessage";
        static ILog logger = LogManager.GetLogger<DisconnectMessageBehavior>();
    }
}