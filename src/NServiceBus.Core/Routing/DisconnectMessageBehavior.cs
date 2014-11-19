namespace NServiceBus.Routing
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class DisconnectMessageBehavior : IBehavior<IncomingContext>
    {
        readonly NoMessageBacklogNotifier monitor;

        public DisconnectMessageBehavior(NoMessageBacklogNotifier monitor)
        {
            this.monitor = monitor;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            var transportMessage = context.PhysicalMessage;

            if (!transportMessage.IsControlMessage() || !IsDisconnectMessage(transportMessage))
            {
                monitor.ResetTimer();
                next();
                return;
            }

            var callbackUrl = CallbackUrl(transportMessage);

            monitor.StartTimer(callbackUrl);
        }

        const string DisconnectHeader = "NServiceBus.DisconnectMessage";
        const string CallbackUrlHeader = "NServiceBus.DisconnectMessage.CallbackUrl";

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

        public class DisconnectMessageRegistration : RegisterStep
        {
            public DisconnectMessageRegistration()
                : base("DisconnectMessage", typeof(DisconnectMessageBehavior), "Armes itself to notify when it is safe to disconnect.")
            {
                InsertBefore(WellKnownStep.MutateIncomingTransportMessage);
                InsertAfter(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}