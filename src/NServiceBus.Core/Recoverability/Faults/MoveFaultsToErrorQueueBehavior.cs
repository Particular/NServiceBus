namespace NServiceBus
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Transports;

    class MoveFaultsToErrorQueueBehavior : ForkConnector<ITransportReceiveContext, IFaultContext>
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, string localAddress, TransportTransactionMode transportTransactionMode, FailureInfoStorage failureInfoStorage)
        {
            this.criticalError = criticalError;
            this.localAddress = localAddress;
            this.transportTransactionMode = transportTransactionMode;
            this.failureInfoStorage = failureInfoStorage;
        }

        bool RunningWithTransactions => transportTransactionMode != TransportTransactionMode.None;

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IFaultContext, Task> fork)
        {
            var message = context.Message;

            var failureInfo = failureInfoStorage.GetFailureInfoForMessage(message.MessageId);

            if (failureInfo.MoveToErrorQueue)
            {
                await MoveMessageToErrorQueue(context, fork, message, failureInfo.Exception).ConfigureAwait(false);

                return;
            }

            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (RunningWithTransactions)
                {
                    failureInfoStorage.MarkForMovingToErrorQueue(message.MessageId, ExceptionDispatchInfo.Capture(ex));

                    context.AbortReceiveOperation();
                }
                else
                {
                    await MoveMessageToErrorQueue(context, fork, message, ex).ConfigureAwait(false);
                }
            }
        }

        async Task MoveMessageToErrorQueue(ITransportReceiveContext context, Func<IFaultContext, Task> fork, IncomingMessage message, Exception exception)
        {
            try
            {
                Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", exception);

                message.RevertToOriginalBodyIfNeeded();

                message.Headers.Remove(Headers.Retries);
                message.Headers.Remove(Headers.FLRetries);

                var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
                var faultContext = this.CreateFaultContext(context, outgoingMessage, localAddress, exception);

                failureInfoStorage.ClearFailureInfoForMessage(message.MessageId);

                await fork(faultContext).ConfigureAwait(false);

                await context.RaiseNotification(new MessageFaulted(message, exception)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                criticalError.Raise("Failed to forward message to error queue", ex);

                throw;
            }
        }

        CriticalError criticalError;
        FailureInfoStorage failureInfoStorage;
        string localAddress;
        TransportTransactionMode transportTransactionMode;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();
    }
}