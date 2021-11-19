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
        public DelayedRetryExecutor(IMessageDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        public async Task<int> Retry(ErrorContext errorContext, TimeSpan delay, CancellationToken cancellationToken = default)
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

            var transportOperations = new TransportOperations(new TransportOperation(outgoingMessage, messageDestination, dispatchProperties));

            await dispatcher.Dispatch(transportOperations, errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);

            return currentDelayedRetriesAttempt;
        }

        IMessageDispatcher dispatcher;
    }
}
