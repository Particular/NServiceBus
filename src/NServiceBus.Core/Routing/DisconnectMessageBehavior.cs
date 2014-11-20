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

            var callbackUrl = CallbackUrl(transportMessage);

            logger.Info("Received a notify for safe disconnect message, starting the timer.");
            monitor.StartTimer(callbackUrl);
        }

        string CallbackUrl(TransportMessage msg)
        {
            string url;
            msg.Headers.TryGetValue(CallbackUrlHeader, out url);
            return url;
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
        const string CallbackUrlHeader = "NServiceBus.DisconnectMessage.CallbackUrl";
        readonly NoMessageBacklogNotifier monitor;
        static ILog logger = LogManager.GetLogger<DisconnectMessageBehavior>();
    }
}