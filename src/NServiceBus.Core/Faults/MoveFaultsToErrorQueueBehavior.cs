namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Hosting;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class MoveFaultsToErrorQueueBehavior : PhysicalMessageProcessingStageBehavior
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, IPipelineBase<DispatchContext> dispatchPipeline, HostInformation hostInformation, BusNotifications notifications, string errorQueueAddress)
        {
            this.criticalError = criticalError;
            this.dispatchPipeline = dispatchPipeline;
            this.hostInformation = hostInformation;
            this.notifications = notifications;
            this.errorQueueAddress = errorQueueAddress;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                try
                {
                    var message = context.GetPhysicalMessage();

                    Logger.Error(string.Format("Moving message '{0}' to the error queue because processing failed due to an exception:", message.Id), exception);

                    message.RevertToOriginalBodyIfNeeded();

                    message.SetExceptionHeaders(exception, PipelineInfo.TransportAddress);

                    message.Headers.Remove(Headers.Retries);

                    //todo: move this to a error pipeline
                    message.Headers[Headers.HostId] = hostInformation.HostId.ToString("N");
                    message.Headers[Headers.HostDisplayName] = hostInformation.DisplayName;

            
                    var dispatchContext = new DispatchContext(new OutgoingMessage(message.Id, message.Headers, message.Body), context);

                    context.Set<RoutingStrategy>(new DirectToTargetDestination(errorQueueAddress));
                    
                    await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);

                    notifications.Errors.InvokeMessageHasBeenSentToErrorQueue(message,exception);
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }
        }

        CriticalError criticalError;
        IPipelineBase<DispatchContext> dispatchPipeline;
        HostInformation hostInformation;
        BusNotifications notifications;
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