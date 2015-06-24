namespace NServiceBus.DelayedDelivery
{
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Timeout;
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

        public void CancelDeferredMessages(string messageKey, BehaviorContext context)
        {
            var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            controlMessage.Headers[Headers.SagaId] = messageKey;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = bool.TrueString;

            var dispatchContext = new DispatchContext(controlMessage, context);

            context.Set<RoutingStrategy>(new DirectToTargetDestination(timeoutManagerAddress));

            dispatchPipeline.Invoke(dispatchContext);
        }

        string timeoutManagerAddress;
        IPipelineBase<DispatchContext> dispatchPipeline;

    }
}