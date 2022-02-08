namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using DelayedDelivery;
    using Logging;
    using Pipeline;
    using Recoverability;
    using Routing;
    using Transport;

    /// <summary>
    /// Indicates recoverability is required to delay retry the current message.
    /// </summary>
    public class DelayedRetry : RecoverabilityAction
    {
        /// <summary>
        /// Creates the action with the requested delay.
        /// </summary>
        protected internal DelayedRetry(TimeSpan delay) => Delay = delay;

        /// <summary>
        /// The retry delay.
        /// </summary>
        public TimeSpan Delay { get; }

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;

        /// <inheritdoc />
        public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
        {
            var errorContext = context.ErrorContext;
            var message = errorContext.Message;

            Logger.Warn($"Delayed Retry will reschedule message '{message.MessageId}' after a delay of {Delay} because of an exception:", errorContext.Exception);

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var currentDelayedRetriesAttempt = message.GetDelayedDeliveriesPerformed() + 1;

            if (context is IRecoverabilityActionContextNotifications notifications)
            {
                notifications.Add(new MessageToBeRetried(
                    attempt: currentDelayedRetriesAttempt,
                    delay: Delay,
                    immediateRetry: false,
                    errorContext: errorContext));
            }

            outgoingMessage.SetCurrentDelayedDeliveries(currentDelayedRetriesAttempt);
            outgoingMessage.SetDelayedDeliveryTimestamp(DateTimeOffset.UtcNow);

            var routingContext = context.CreateRoutingContext(outgoingMessage, new UnicastRoutingStrategy(errorContext.ReceiveAddress));
            routingContext.Extensions.Set(new DispatchProperties
            {
                DelayDeliveryWith = new DelayDeliveryWith(Delay)
            });
            return new[] { routingContext };
        }

        static readonly ILog Logger = LogManager.GetLogger<DelayedRetry>();
    }
}