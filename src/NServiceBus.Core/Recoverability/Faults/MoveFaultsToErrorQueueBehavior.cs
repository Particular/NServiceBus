namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Hosting;
    using Logging;
    using Pipeline;
    using Pipeline.Contexts;
    using Routing;
    using TransportDispatch;
    using Transports;

    class MoveFaultsToErrorQueueBehavior : Behavior<TransportReceiveContext>
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, IPipelineBase<RoutingContext> dispatchPipeline, HostInformation hostInformation, BusNotifications notifications, string errorQueueAddress)
        {
            this.criticalError = criticalError;
            this.dispatchPipeline = dispatchPipeline;
            this.hostInformation = hostInformation;
            this.notifications = notifications;
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


                    var dispatchContext = new RoutingContext(new OutgoingMessage(message.MessageId, message.Headers, message.Body),
                        new DirectToTargetDestination(errorQueueAddress),
                        context);

                    await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);

                    notifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message, exception);
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }
        }

        CriticalError criticalError;
        IPipelineBase<RoutingContext> dispatchPipeline;
        string errorQueueAddress;
        HostInformation hostInformation;
        BusNotifications notifications;

        public class Registration : RegisterStep
        {
            public Registration()
                : base("MoveFaultsToErrorQueue", typeof(MoveFaultsToErrorQueueBehavior), "Moved failing messages to the configured error queue")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("SecondLevelRetries");
            }
        }

        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();
    }
}