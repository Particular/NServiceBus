namespace NServiceBus.Recoverability.Faults
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class MoveFaultsToErrorQueueHandler
    {
        public MoveFaultsToErrorQueueHandler(CriticalError criticalError, IPipelineBase<RoutingContext> dispatchPipeline, HostInformation hostInformation, BusNotifications notifications, string errorQueueAddress)
        {
            this.criticalError = criticalError;
            this.dispatchPipeline = dispatchPipeline;
            this.hostInformation = hostInformation;
            this.notifications = notifications;
            this.errorQueueAddress = errorQueueAddress;
        }

        public async Task MoveToErrorQueue(TransportReceiveContext context, Exception exception, string transportAddress)
        {
            try
            {
                var message = context.Message;

                Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", exception);

                message.RevertToOriginalBodyIfNeeded();

                message.SetExceptionHeaders(exception, transportAddress);

                message.Headers.Remove(Headers.Retries);

                //todo: move this to a error pipeline
                message.Headers[Headers.HostId] = hostInformation.HostId.ToString("N");
                message.Headers[Headers.HostDisplayName] = hostInformation.DisplayName;

                var dispatchContext = new RoutingContext(
                    new OutgoingMessage(message.MessageId, message.Headers, message.Body),
                    new UnicastRoutingStrategy(errorQueueAddress),
                    context);

                await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);

                notifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, exception);
            }
            catch (Exception e)
            {
                criticalError.Raise("Failed to forward message to error queue", e);
                throw;
            }
        }

        CriticalError criticalError;
        IPipelineBase<RoutingContext> dispatchPipeline;
        HostInformation hostInformation;
        BusNotifications notifications;
        string errorQueueAddress;

        static ILog Logger = LogManager.GetLogger<RecoverabilityBehavior>();
    }
}