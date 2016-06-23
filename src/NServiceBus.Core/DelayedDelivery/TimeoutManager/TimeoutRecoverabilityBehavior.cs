namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Faults;
    using Logging;
    using Routing;
    using Transports;

    class TimeoutRecoverabilityBehavior
    {
        public TimeoutRecoverabilityBehavior(string errorQueueAddress, string localAddress, IDispatchMessages dispatcher, CriticalError criticalError)
        {
            this.localAddress = localAddress;
            this.errorQueueAddress = errorQueueAddress;
            this.dispatcher = dispatcher;
            this.criticalError = criticalError;
        }

        public async Task<bool> Invoke(PushContext context, Exception exception, int numRetries)
        {
            if (numRetries <= MaxNumberOfFailedRetries)
            {
                return true;
            }

            await MoveToErrorQueue(context, exception).ConfigureAwait(false);

            return false;
        }

        async Task MoveToErrorQueue(PushContext context, Exception exception)
        {
            try
            {
                Logger.Error($"Moving timeout message '{context.MessageId}' from '{localAddress}' to '{errorQueueAddress}' because processing failed due to an exception:", exception);

                var body = new byte[context.BodyStream.Length];
                await context.BodyStream.ReadAsync(body, 0, body.Length).ConfigureAwait(false);

                var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, body);

                ExceptionHeaderHelper.SetExceptionHeaders(outgoingMessage.Headers, exception);

                outgoingMessage.Headers[FaultsHeaderKeys.FailedQ] = localAddress;

                var addressTag = new UnicastAddressTag(errorQueueAddress);

                var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, addressTag));

                await dispatcher.Dispatch(transportOperations, context.Context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                criticalError.Raise("Failed to forward failed timeout message to error queue", ex);
                throw;
            }
        }

        CriticalError criticalError;
        IDispatchMessages dispatcher;
        string errorQueueAddress;

        string localAddress;

        const int MaxNumberOfFailedRetries = 4;

        static ILog Logger = LogManager.GetLogger<TimeoutRecoverabilityBehavior>();
    }
}