namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Routing;
    using Transports;

    class MoveFaultsToErrorQueueBehavior
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError)
        {
            this.criticalError = criticalError;
        }

        public async Task Invoke(string errorQueue, IncomingMessage message, Exception exception, IDispatchMessages dispatcher, ContextBag context)
        {
            try
            {
                Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", exception);

                message.Headers.Remove(Headers.Retries);
                message.Headers.Remove(Headers.FLRetries);

                var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
                var transportOperation = new TransportOperation(outgoingMessage, new UnicastAddressTag(errorQueue));
                var transportOperations = new TransportOperations(transportOperation);

                //HINT: this holds and expension point that we need to preserve
                //var faultContext = this.CreateFaultContext(context, outgoingMessage, localAddress, exception);

                await dispatcher.Dispatch(transportOperations, context).ConfigureAwait(false);

                //await context.RaiseNotification(new MessageFaulted(message, exception)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                criticalError.Raise("Failed to forward message to error queue", ex);

                throw;
            }
        }

        CriticalError criticalError;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();
    }
}