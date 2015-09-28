namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Forwarding;
    using Routing;
    using Pipeline;
    using TransportDispatch;

    class ForwardingToDispatchConnector : StageConnector<ForwardingContext,DispatchContext>
    {
        public override Task Invoke(ForwardingContext context, Func<DispatchContext, Task> next)
        {
            return next(new DispatchContext(context.Message,new DirectToTargetDestination(context.Address), context));
        }
    }
}