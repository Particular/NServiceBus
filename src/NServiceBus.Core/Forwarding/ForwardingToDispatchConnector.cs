namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Pipeline;
    using TransportDispatch;

    class ForwardingToDispatchConnector : StageConnector<ForwardingContext,RoutingContext>
    {
        public override Task Invoke(ForwardingContext context, Func<RoutingContext, Task> next)
        {
            return next(new RoutingContextImpl(context.Message, new UnicastRoutingStrategy(context.Address), context));
        }
    }
}