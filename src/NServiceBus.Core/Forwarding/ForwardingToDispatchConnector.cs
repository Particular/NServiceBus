namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Forwarding;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class ForwardingToDispatchConnector:StageConnector<ForwardingContext,DispatchContext>
    {
        public override Task Invoke(ForwardingContext context, Func<DispatchContext, Task> next)
        {
            return next(new DispatchContext(context.Get<OutgoingMessage>(),context));
        }
    }
}