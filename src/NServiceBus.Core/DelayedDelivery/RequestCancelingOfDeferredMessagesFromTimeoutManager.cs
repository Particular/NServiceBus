namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class RequestCancelingOfDeferredMessagesFromTimeoutManager : ICancelDeferredMessages
    {
        public RequestCancelingOfDeferredMessagesFromTimeoutManager(string timeoutManagerAddress)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
        }

        public Task CancelDeferredMessages(string messageKey, IBehaviorContext context)
        {
            var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            controlMessage.Headers[Headers.SagaId] = messageKey;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = bool.TrueString;

            var dispatchContext = new RoutingContext(controlMessage, new UnicastRoutingStrategy(timeoutManagerAddress), context);

            var cache = context.Extensions.Get<IPipelineCache>();
            var dispatchPipeline = cache.Pipeline<IRoutingContext>();
            return dispatchPipeline.Invoke(dispatchContext);
        }

        string timeoutManagerAddress;
    }
}