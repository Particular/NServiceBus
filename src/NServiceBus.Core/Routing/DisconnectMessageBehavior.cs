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
        public DisconnectMessageBehavior(NoMessageBacklogNotifier monitor)
        {
            this.monitor = monitor;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            var transportMessage = context.PhysicalMessage;

            if (!transportMessage.IsControlMessage() && !IsDisconnectMessage(transportMessage))
            {
                monitor.ResetTimer();
                next();
                return;
            }

            logger.Info("Received a notify for safe disconnect message, starting the timer.");
            monitor.StartTimer(transportMessage.Headers);
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
        readonly NoMessageBacklogNotifier monitor;
        static ILog logger = LogManager.GetLogger<DisconnectMessageBehavior>();
    }
}