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

        public RequestCancelingOfDeferredMessagesFromTimeoutManager(string timeoutManagerAddress)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
        }

        public Task CancelDeferredMessages(string messageKey, BehaviorContext context, IPipeInlet<RoutingContext> routingPipe)
        {
            var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            controlMessage.Headers[Headers.SagaId] = messageKey;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = bool.TrueString;

            var dispatchContext = new RoutingContext(controlMessage, new UnicastRoutingStrategy(timeoutManagerAddress), context);
            
            return routingPipe.Put(dispatchContext);
        }

        string timeoutManagerAddress;
    }
}