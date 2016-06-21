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
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, string localAddress, TransportTransactionMode transportTransactionMode, FailureInfoStorage failureInfoStorage)
        {
            this.criticalError = criticalError;
            this.localAddress = localAddress;
            this.transportTransactionMode = transportTransactionMode;
            this.failureInfoStorage = failureInfoStorage;
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

                message.RevertToOriginalBodyIfNeeded();

                message.Headers.Remove(Headers.Retries);
                message.Headers.Remove(Headers.FLRetries);

                
                failureInfoStorage.ClearFailureInfoForMessage(message.MessageId);

                //var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
                //await fork(faultContext).ConfigureAwait(false);
                //   context.Message.SetExceptionHeaders(context.Exception, context.SourceQueueAddress);

                //context.AddFaultData(Headers.HostId, hostInfo.HostId.ToString("N"));
                //context.AddFaultData(Headers.HostDisplayName, hostInfo.DisplayName);

                //context.AddFaultData(Headers.ProcessingMachine, RuntimeEnvironment.MachineName);
                //context.AddFaultData(Headers.ProcessingEndpoint, endpointName);
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