namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Forwarding;
    using Routing;
    using Pipeline;
    using TransportDispatch;

    class ForwardingToRoutingConnector : StageConnector<ForwardingContext,RoutingContext>
    {
        public override Task Invoke(ForwardingContext context, Func<RoutingContext, Task> next)
        {
            return next(new RoutingContext(context.Message, new UnicastRoutingStrategy(context.Address), context));
        }
    }
}