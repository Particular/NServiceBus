namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Routing;
    using Transport;
    using Unicast.Transport;

    class RequestCancelingOfDeferredMessagesFromTimeoutManager : ICancelDeferredMessages
    {
        public RequestCancelingOfDeferredMessagesFromTimeoutManager(string timeoutManagerAddress)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
        }

        public Task CancelDeferredMessages(string messageKey, IBehaviorContext context, CancellationToken cancellationToken)
        {
            var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            controlMessage.Headers[Headers.SagaId] = messageKey;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = bool.TrueString;

            var dispatchContext = new RoutingContext(controlMessage, new UnicastRoutingStrategy(timeoutManagerAddress), context);

            return dispatchContext.InvokePipeline<IRoutingContext>(cancellationToken);
        }

        string timeoutManagerAddress;
    }
}