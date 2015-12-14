namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Pipeline;
    using TransportDispatch;

    class ForwardingToRoutingConnector : StageConnector<IForwardingContext, IRoutingContext>
    {
        public override Task Invoke(IForwardingContext context, Func<IRoutingContext, Task> next)
        {
            return next(new RoutingContext(context.Message, new UnicastRoutingStrategy(context.Address), context));
        }
    }
}