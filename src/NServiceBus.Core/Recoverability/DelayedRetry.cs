namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.DelayedDelivery;
    using NServiceBus.Logging;
    using NServiceBus.Routing;
    using NServiceBus.Transport;

    /// <summary>
    /// Indicates recoverability is required to delay retry the current message.
    /// </summary>
    public sealed class DelayedRetry : RecoverabilityAction
    {
        internal DelayedRetry(TimeSpan delay)
        {
            Delay = delay;
        }

        /// <summary>
        /// The retry delay.
        /// </summary>
        public TimeSpan Delay { get; }

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;

        /// <summary>
        /// Executes the recoverability action.
        /// </summary>
        public override IEnumerable<TransportOperation> Execute(
            ErrorContext errorContext,
            IDictionary<string, string> metadata)
        {
            var message = errorContext.Message;

            Logger.Warn($"Delayed Retry will reschedule message '{message.MessageId}' after a delay of {Delay} because of an exception:", errorContext.Exception);

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var currentDelayedRetriesAttempt = message.GetDelayedDeliveriesPerformed() + 1;

            outgoingMessage.SetCurrentDelayedDeliveries(currentDelayedRetriesAttempt);
            outgoingMessage.SetDelayedDeliveryTimestamp(DateTimeOffset.UtcNow);

            var dispatchProperties = new DispatchProperties
            {
                DelayDeliveryWith = new DelayDeliveryWith(Delay)
            };

            var messageDestination = new UnicastAddressTag(errorContext.ReceiveAddress);

            yield return new TransportOperation(outgoingMessage, messageDestination, dispatchProperties);
        }


        static ILog Logger = LogManager.GetLogger<DelayedRetry>();
    }
}