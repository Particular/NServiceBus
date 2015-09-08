namespace NServiceBus
{
    using System;
    using NServiceBus.EndpointControl;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class DetectDisconnectRequestBehavior : Behavior<TransportReceiveContext>
    {
        public NoMessageBacklogNotifier Monitor { get; set; }

        public override void Invoke(TransportReceiveContext context, Action next)
        {
            var transportMessage = context.GetPhysicalMessage();

            if (IsDisconnectMessage(transportMessage))
            {
                logger.Info("Received a notify for safe disconnect message, starting the timer.");
                Monitor.StartTimer(transportMessage.Headers);
                return;
            }

            next();
        }

        bool IsDisconnectMessage(TransportMessage msg)
        {
            if (TransportMessageExtensions.IsControlMessage(msg.Headers) && msg.Headers.ContainsKey(DisconnectHeader))
            {
                return true;
            }

            return false;
        }

        const string DisconnectHeader = "NServiceBus.DisconnectMessage";
        static ILog logger = LogManager.GetLogger<DetectDisconnectRequestBehavior>();
    }
}