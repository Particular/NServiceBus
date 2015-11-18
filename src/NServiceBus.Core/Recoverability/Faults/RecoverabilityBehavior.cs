namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Recoverability.Faults;
    using NServiceBus.Recoverability.FirstLevelRetries;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class RecoverabilityBehavior : Behavior<TransportReceiveContext>
    {
        public RecoverabilityBehavior(
            CriticalError criticalError,
            IPipelineBase<RoutingContext> dispatchPipeline,
            HostInformation hostInformation,
            BusNotifications notifications,
            string errorQueueAddress,
            FaultsStatusStorage faultsStatusStorage,
            FirstLevelRetriesHandler flrHandler,
            SecondLevelRetriesHandler slrHandler)
        {
            this.criticalError = criticalError;
            this.dispatchPipeline = dispatchPipeline;
            this.hostInformation = hostInformation;
            this.notifications = notifications;
            this.errorQueueAddress = errorQueueAddress;
            this.faultsStatusStorage = faultsStatusStorage;
            this.flrHandler = flrHandler;
            this.slrHandler = slrHandler;
        }

        public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
        {
            //TODO: this is bad :/ - we probably need to have a spearate class for timeout manager and separate suit of tests
            var suppressSlr = PipelineInfo.Name == "Timeout Message Processor" || PipelineInfo.Name == "Timeout Dispatcher Processor";

            var uniqueMessageId = PipelineInfo.Name + context.Message.MessageId;

            //TODO: Failure storage should be managed here
            ProcessingFailureInfo failureInfo;

            if (flrHandler.TryHandle(uniqueMessageId, context.Message, out failureInfo) == false)
            {
                TimeSpan delay;
                int currentRetry;

                //TODO: what should happen when SLR fails ???
                if (suppressSlr == false && slrHandler.ShouldPerformSlr(context.Message, failureInfo.Exception, out delay, out currentRetry))
                {
                    try
                    {
                        await slrHandler.QueueForDelayedDelivery(context, currentRetry, delay, failureInfo.Exception).ConfigureAwait(false);

                        return;
                    }
                    catch(Exception ex)
                    {
                        Logger.Warn($"Failed to perform SLR for message '{context.Message.MessageId}'.", ex);

                        await TryMovingToFaultsQueue(context, failureInfo, uniqueMessageId);

                        return;
                    }
                }

                context.Message.Headers.Remove(Headers.Retries);

                Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", context.Message.MessageId);

                await TryMovingToFaultsQueue(context, failureInfo, uniqueMessageId);

                return;
            }

            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                flrHandler.MarkFailure(uniqueMessageId, ex);

                throw new MessageProcessingAbortedException();
            }
        }

        async Task TryMovingToFaultsQueue(TransportReceiveContext context, ProcessingFailureInfo failureInfo, string uniqueMessageId)
        {
            try
            {
                await MoveToErrorQueue(context, context.Message, failureInfo.Exception).ConfigureAwait(false);

                faultsStatusStorage.ClearExceptions(uniqueMessageId);

                return;
            }
            catch (Exception ex)
            {
                criticalError.Raise("Failed to forward message to error queue", ex);
                throw;
            }
        }

        async Task MoveToErrorQueue(TransportReceiveContext context, IncomingMessage message, Exception exception)
        {
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
        FaultsStatusStorage faultsStatusStorage;
        readonly FirstLevelRetriesHandler flrHandler;
        readonly SecondLevelRetriesHandler slrHandler;
        static ILog Logger = LogManager.GetLogger<RecoverabilityBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("Recoverability", typeof(RecoverabilityBehavior), "Moved failing messages to the configured error queue")
            {
                //TODO: this should probably be first behavior in the pipeline
            }
        }
    }
}