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

            Exception exception;
            var uniqueMessageId = PipelineInfo.Name + context.Message.MessageId;

            if (faultsStatusStorage.TryGetException(uniqueMessageId, out exception))
            {
                try
                {
                    await MoveToErrorQueue(context, context.Message, exception).ConfigureAwait(false);

                    faultsStatusStorage.ClearExceptions(uniqueMessageId);

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
                if (suppressSlr == false && slrHandler.TryGetException(uniqueMessageId, out exception))
                {
                    slrHandler.ClearException(uniqueMessageId);

                    TimeSpan delay;
                    int currentRetry;

                    if (slrHandler.ShouldPerformSlr(context.Message, exception, out delay, out currentRetry))
                    {
                        await slrHandler.QueueForDelayedDelivery(context, currentRetry, delay, exception).ConfigureAwait(false);

                        return;
                    }

                    context.Message.Headers.Remove(Headers.Retries);

                    Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", context.Message.MessageId);

                    throw exception;
                }

                try
                {
                    await next().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (flrHandler.TryHandle(PipelineInfo.Name, context, ex))
                        {
                            throw new MessageProcessingAbortedException();
                        }

                        throw;
                    }
                    catch (MessageProcessingAbortedException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        if (!suppressSlr && slrHandler.IsEnabled)
                        {
                            //Mark for processing on next receive
                            slrHandler.AddException(uniqueMessageId, e);

                            throw new MessageProcessingAbortedException();
                        }

                        throw;
                    }
                }
            }
            catch (MessageProcessingAbortedException)
            {
                throw;
            }
            catch (Exception e)
            {
                faultsStatusStorage.AddException(uniqueMessageId, e);

                throw new MessageProcessingAbortedException();
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