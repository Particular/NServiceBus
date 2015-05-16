namespace NServiceBus
{
    using System;
    using NServiceBus.Forwarding;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class ForwardingToDispatchConnector:StageConnector<ForwardingContext,DispatchContext>
    {
        public override void Invoke(ForwardingContext context, Action<DispatchContext> next)
        {
            next(new DispatchContext(context.Get<OutgoingMessage>(),context));
        }
    }
}