namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class MoveFaultsToErrorQueueBehavior : Behavior<TransportReceiveContext>
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, IPipelineBase<RoutingContext> dispatchPipeline, HostInformation hostInformation, FailedMessageAction failedMessageAction, string errorQueueAddress)
        {
            this.criticalError = criticalError;
            this.dispatchPipeline = dispatchPipeline;
            this.hostInformation = hostInformation;
            this.failedMessageAction = failedMessageAction;
            this.errorQueueAddress = errorQueueAddress;
        }

        public override async Task Invoke(TransportReceiveContext context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageProcessingAbortedException)
            {
                throw;
            }
            catch (Exception exception)
            {
                try
                {
                    var message = context.Message;

                    Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", exception);

                    message.RevertToOriginalBodyIfNeeded();

                    message.SetExceptionHeaders(exception, PipelineInfo.TransportAddress);

                    message.Headers.Remove(Headers.Retries);

                    //todo: move this to a error pipeline
                    message.Headers[Headers.HostId] = hostInformation.HostId.ToString("N");
                    message.Headers[Headers.HostDisplayName] = hostInformation.DisplayName;

                    var dispatchContext = new RoutingContextImpl(new OutgoingMessage(message.MessageId, message.Headers, message.Body),
                        new UnicastRoutingStrategy(errorQueueAddress),
                        context);

                    await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);
                    InvokeNotification(message, exception);
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }
        }

        void InvokeNotification(IncomingMessage message, Exception exception)
        {
            var failedMessage = new FailedMessage(message.MessageId, message.Headers, message.Body, exception);
            failedMessageAction(failedMessage);
        }

        CriticalError criticalError;
        IPipelineBase<RoutingContext> dispatchPipeline;
        HostInformation hostInformation;
        FailedMessageAction failedMessageAction;
        string errorQueueAddress;
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