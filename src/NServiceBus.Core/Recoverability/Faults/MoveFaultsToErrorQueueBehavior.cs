namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class MoveFaultsToErrorQueueBehavior : ForkConnector<ITransportReceiveContext, IFaultContext>
    {
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, BusNotifications notifications, string errorQueueAddress, string localAddress)

        {
            this.criticalError = criticalError;
            this.notifications = notifications;
            this.errorQueueAddress = errorQueueAddress;
            this.localAddress = localAddress;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IFaultContext, Task> fork)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                try
                {
                    Logger.Error($"Moving message '{context.MessageId}' to the error queue because processing failed due to an exception:", exception);

                    context.RevertToOriginalBodyIfNeeded();

                    context.SetExceptionHeaders(exception, localAddress);

                    context.Headers.Remove(Headers.Retries);

                    var outgoingMessage = new OutgoingMessage(context.MessageId, context.Headers, context.Body);
                    var faultContext = this.CreateFaultContext(context, outgoingMessage, errorQueueAddress, exception);

                    await fork(faultContext).ConfigureAwait(false);
                    
                    notifications.Errors.InvokeMessageHasBeenSentToErrorQueue(context, exception);
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }
        }

        CriticalError criticalError;
        BusNotifications notifications;
        string errorQueueAddress;
        string localAddress;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();

        public class Registration : RegisterStep
        {
            public Registration(string errorQueueAddress, string localAddress)
                : base("MoveFaultsToErrorQueue", typeof(MoveFaultsToErrorQueueBehavior), "Moved failing messages to the configured error queue", b =>
                {
                    return new MoveFaultsToErrorQueueBehavior(
                        b.Build<CriticalError>(),
                        b.Build<BusNotifications>(),
                        errorQueueAddress,
                        localAddress);
                })
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("SecondLevelRetries");
            }
        }
    }
}