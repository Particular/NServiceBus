namespace NServiceBus
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Transports;

    class MoveFaultsToErrorQueueBehavior : Behavior<ITransportReceiveContext>
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError,
            TransportTransactionMode transportTransactionMode,
            FailureInfoStorage failureInfoStorage,
            RecoveryActionExecutor recoveryActionExecutor)
        {
            this.criticalError = criticalError;
            this.transportTransactionMode = transportTransactionMode;
            this.failureInfoStorage = failureInfoStorage;
            this.recoveryActionExecutor = recoveryActionExecutor;
        }

        bool RunningWithTransactions => transportTransactionMode != TransportTransactionMode.None;

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
        {
            var message = context.Message;

            var failureInfo = failureInfoStorage.GetFailureInfoForMessage(message.MessageId);

            if (failureInfo.MoveToErrorQueue)
            {
                await MoveMessageToErrorQueue(context, message, failureInfo.Exception).ConfigureAwait(false);

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
                    await MoveMessageToErrorQueue(context, message, ex).ConfigureAwait(false);
                }
            }
        }

        async Task MoveMessageToErrorQueue(ITransportReceiveContext context, IncomingMessage message, Exception exception)
        {
            try
            {
                Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", exception);

                failureInfoStorage.ClearFailureInfoForMessage(message.MessageId);

                await recoveryActionExecutor.MoveToErrorQueue(message, exception, context.Extensions).ConfigureAwait(true);

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
        readonly RecoveryActionExecutor recoveryActionExecutor;
        TransportTransactionMode transportTransactionMode;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();
    }
}