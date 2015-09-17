namespace NServiceBus.DelayedDelivery
{
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery.TimeoutManager;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class RequestCancelingOfDeferredMessagesFromTimeoutManager : ICancelDeferredMessages
    {

        public RequestCancelingOfDeferredMessagesFromTimeoutManager(string timeoutManagerAddress, IPipelineBase<DispatchContext> dispatchPipeline)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
            this.dispatchPipeline = dispatchPipeline;
        }

        public Task CancelDeferredMessages(string messageKey, BehaviorContext context)
        {
            var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            controlMessage.Headers[Headers.SagaId] = messageKey;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = bool.TrueString;

            var dispatchContext = new DispatchContext(controlMessage, context);

            context.Set<RoutingStrategy>(new DirectToTargetDestination(timeoutManagerAddress));

            return dispatchPipeline.Invoke(dispatchContext);
        }

        string timeoutManagerAddress;
        IPipelineBase<DispatchContext> dispatchPipeline;

    }
}