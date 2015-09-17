namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class DispatchTerminator : PipelineTerminator<DispatchContext>
    {
        public DispatchTerminator(IDispatchMessages dispatcher, DispatchStrategy defaultDispatchStrategy)
        {
            this.dispatcher = dispatcher;
            this.defaultDispatchStrategy = defaultDispatchStrategy;
        }

        protected override Task Terminate(DispatchContext context)
        {
            DispatchStrategy dispatchStrategy;

            if (!context.TryGet(out dispatchStrategy))
            {
                dispatchStrategy = defaultDispatchStrategy;
            }
            var routingStrategy = context.GetRoutingStrategy();

            return dispatchStrategy.Dispatch(dispatcher, context.Get<OutgoingMessage>(), routingStrategy, context.GetDeliveryConstraints(), context, DispatchConsistency.Default);
        }

        IDispatchMessages dispatcher;
        DispatchStrategy defaultDispatchStrategy;
    }
}