namespace NServiceBus.DelayedDelivery
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class NoOpCanceling : ICancelDeferredMessages
    {
        public Task CancelDeferredMessages(string messageKey, BehaviorContext context, IPipeInlet<RoutingContext> downpipe)
        {
            //no-op
            return TaskEx.Completed;
        }
    }
}