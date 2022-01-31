namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using Routing;
    using Transport;

    class DelayedRetryExecutor
    {
        public async Task Retry(
            ErrorContext errorContext,
            TimeSpan delay,
            Func<TransportOperation, CancellationToken, Task> dispatchAction,
            CancellationToken cancellationToken = default)
        {
            var message = errorContext.Message;
            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var currentDelayedRetriesAttempt = message.GetDelayedDeliveriesPerformed() + 1;

            outgoingMessage.SetCurrentDelayedDeliveries(currentDelayedRetriesAttempt);
            outgoingMessage.SetDelayedDeliveryTimestamp(DateTimeOffset.UtcNow);

            var dispatchProperties = new DispatchProperties
            {
                DelayDeliveryWith = new DelayDeliveryWith(delay)
            };

            var messageDestination = new UnicastAddressTag(errorContext.ReceiveAddress);

            await dispatchAction(new TransportOperation(outgoingMessage, messageDestination, dispatchProperties), cancellationToken).ConfigureAwait(false);
        }
    }
}
