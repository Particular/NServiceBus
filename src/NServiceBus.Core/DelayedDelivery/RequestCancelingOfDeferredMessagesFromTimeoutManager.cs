namespace NServiceBus.DelayedDelivery
{
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using TimeoutManager;
    using TransportDispatch;
    using Transports;
    using Unicast.Transport;

    class RequestCancelingOfDeferredMessagesFromTimeoutManager : ICancelDeferredMessages
    {
        public RequestCancelingOfDeferredMessagesFromTimeoutManager(string timeoutManagerAddress, IPipelineBase<RoutingContext> dispatchPipeline)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
            this.dispatchPipeline = dispatchPipeline;
        }

        public Task CancelDeferredMessages(string messageKey, BehaviorContext context)
        {
            var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            controlMessage.Headers[Headers.SagaId] = messageKey;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = bool.TrueString;

            var dispatchContext = new RoutingContext(controlMessage, new DirectToTargetDestination(timeoutManagerAddress), context);

            return dispatchPipeline.Invoke(dispatchContext);
        }

        IPipelineBase<RoutingContext> dispatchPipeline;

        string timeoutManagerAddress;
    }
}