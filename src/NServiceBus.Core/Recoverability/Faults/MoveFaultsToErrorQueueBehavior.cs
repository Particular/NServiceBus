namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.Faults;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class MoveFaultsToErrorQueueBehavior : Behavior<TransportReceiveContext>
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, IPipelineBase<RoutingContext> dispatchPipeline, HostInformation hostInformation, BusNotifications notifications, string errorQueueAddress, FaultsStatusStorage faultsStatusStorage)
        {
            this.criticalError = criticalError;
            this.dispatchPipeline = dispatchPipeline;
            this.hostInformation = hostInformation;
            this.notifications = notifications;
            this.errorQueueAddress = errorQueueAddress;
            this.faultsStatusStorage = faultsStatusStorage;
        }

        public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
        {
            var uniqueMessageId = PipelineInfo.Name + context.Message.MessageId;
            var exception = faultsStatusStorage.GetExceptionForMovingToErrorQueue(uniqueMessageId);

            if (exception != null)
            {
                try
                {
                    await MoveToErrorQueue(context, context.Message, exception, uniqueMessageId);
                    return;
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }
        
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageProcessingAbortedException)
            {
                throw;
            }
            catch (Exception e)
            {
                faultsStatusStorage.MarkForMovingToErrorQueue(uniqueMessageId, e);

                throw new MessageProcessingAbortedException();
            }
        }

        async Task MoveToErrorQueue(TransportReceiveContext context, IncomingMessage message, Exception exception, string uniqueMessageId)
        {
            faultsStatusStorage.MarkAsPickedUpForMovingToErrorQueue(uniqueMessageId);

            Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", exception);

            message.RevertToOriginalBodyIfNeeded();

            message.SetExceptionHeaders(exception, PipelineInfo.TransportAddress);

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

        CriticalError criticalError;
        IPipelineBase<RoutingContext> dispatchPipeline;
        HostInformation hostInformation;
        BusNotifications notifications;
        string errorQueueAddress;
        readonly FaultsStatusStorage faultsStatusStorage;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("MoveFaultsToErrorQueue", typeof(MoveFaultsToErrorQueueBehavior), "Moved failing messages to the configured error queue")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("SecondLevelRetries");
            }
        }
    }
}