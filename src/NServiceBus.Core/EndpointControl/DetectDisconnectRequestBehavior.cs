namespace NServiceBus
{
    using System;
    using NServiceBus.EndpointControl;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class DetectDisconnectRequestBehavior : IBehavior<IncomingContext>
    {
        public NoMessageBacklogNotifier Monitor { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            var transportMessage = context.PhysicalMessage;

            if (IsDisconnectMessage(transportMessage))
            {
                logger.Info("Received a notify for safe disconnect message, starting the timer.");
                Monitor.StartTimer(transportMessage.Headers);
                return;
            }

            Monitor.ResetTimer();
            next();
        }

        bool IsDisconnectMessage(TransportMessage msg)
        {
            if (msg.IsControlMessage() && msg.Headers.ContainsKey(DisconnectHeader))
            {
                return true;
            }

            return false;
        }

        const string DisconnectHeader = "NServiceBus.DisconnectMessage";
        static ILog logger = LogManager.GetLogger<DetectDisconnectRequestBehavior>();
    }
}