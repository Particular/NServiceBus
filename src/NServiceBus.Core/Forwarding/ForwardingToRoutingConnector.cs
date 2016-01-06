namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Pipeline;
    using TransportDispatch;

    class ForwardingToRoutingConnector : StageConnector<IForwardingContext, IRoutingContext>
    {
        public override Task Invoke(IForwardingContext context, Func<IRoutingContext, Task> stage)
        {
            return stage(new RoutingContext(context.Message, new UnicastRoutingStrategy(context.Address), context));
        }
    }
}