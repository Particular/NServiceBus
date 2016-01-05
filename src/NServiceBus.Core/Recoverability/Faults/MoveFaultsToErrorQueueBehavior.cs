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
    using NServiceBus.Settings;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class MoveFaultsToErrorQueueBehavior : Behavior<ITransportReceiveContext>
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, 
            IPipelineBase<IRoutingContext> dispatchPipeline, 
            HostInformation hostInformation,
            Func<FailedMessage, Task> notification, 
            string errorQueueAddress,
            string localAddress)
        {
            this.criticalError = criticalError;
            this.dispatchPipeline = dispatchPipeline;
            this.hostInformation = hostInformation;
            this.notification = notification;
            this.errorQueueAddress = errorQueueAddress;
            this.localAddress = localAddress;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next)
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

                    message.SetExceptionHeaders(exception, localAddress);

                    message.Headers.Remove(Headers.Retries);

                    //todo: move this to a error pipeline
                    message.Headers[Headers.HostId] = hostInformation.HostId.ToString("N");
                    message.Headers[Headers.HostDisplayName] = hostInformation.DisplayName;


                    var dispatchContext = new RoutingContext(new OutgoingMessage(message.MessageId, message.Headers, message.Body), 
                        new UnicastRoutingStrategy(errorQueueAddress), 
                        context);
                    
                    await dispatchPipeline.Invoke(dispatchContext).ConfigureAwait(false);
                    await InvokeNotification(message, exception);
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }
        }

        Task InvokeNotification(IncomingMessage message, Exception exception)
        {
            if (notification == null)
            {
                return TaskEx.Completed;
            }
            var failedMessage = new FailedMessage(message.MessageId, message.Headers, message.Body, exception);
            return notification(failedMessage);
        }

        CriticalError criticalError;
        IPipelineBase<IRoutingContext> dispatchPipeline;
        HostInformation hostInformation;
        Func<FailedMessage, Task> notification;
        string errorQueueAddress;
        string localAddress;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();

        public class Registration : RegisterStep
        {
            public Registration(ReadOnlySettings settings, string localAddress)
                : base("MoveFaultsToErrorQueue", typeof(MoveFaultsToErrorQueueBehavior), "Moved failing messages to the configured error queue", b =>
                {
                    var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(settings);
                    var pipelinesCollection = settings.Get<PipelineConfiguration>();
                    var dispatchPipeline = new PipelineBase<IRoutingContext>(b, settings, pipelinesCollection.MainPipeline);
                    var failedMessageActions = settings.GetFailedMessageNotification();
                    return new MoveFaultsToErrorQueueBehavior(
                        b.Build<CriticalError>(),
                        dispatchPipeline,
                        b.Build<HostInformation>(),
                        failedMessageActions,
                        errorQueue,
                        localAddress);
                })
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("SecondLevelRetries");
            }
        }
    }
}