namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Faults;
    using NServiceBus.Logging;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
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

                    var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
                    var faultContext = this.CreateFaultContext(context, outgoingMessage, errorQueueAddress, exception);

                    await fork(faultContext).ConfigureAwait(false);
                    
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