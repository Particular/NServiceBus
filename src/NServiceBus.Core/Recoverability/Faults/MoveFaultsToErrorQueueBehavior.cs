namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class MoveFaultsToErrorQueueBehavior : Behavior<ITransportReceiveContext>
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, 
            IPipelineBase<IFaultContext> faultPipeline, 
            BusNotifications notifications, 
            string errorQueueAddress,
            string localAddress)
        {
            this.criticalError = criticalError;
            this.faultPipeline = faultPipeline;
            this.notifications = notifications;
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

                    var faultContext = new FaultContext(new OutgoingMessage(message.MessageId, message.Headers, message.Body), errorQueueAddress, exception, context);

                    await faultPipeline.Invoke(faultContext).ConfigureAwait(false);
                    
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
        IPipelineBase<IFaultContext> faultPipeline;
        BusNotifications notifications;
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
                    var dispatchPipeline = new PipelineBase<IFaultContext>(b, settings, pipelinesCollection.MainPipeline);

                    return new MoveFaultsToErrorQueueBehavior(
                        b.Build<CriticalError>(),
                        dispatchPipeline,
                        b.Build<BusNotifications>(),
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