namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Timeout.Core;
    using Transports;

    class DispatchTimeoutBehavior : SatelliteBehavior
    {
        public DispatchTimeoutBehavior(IDispatchMessages dispatcher, IPersistTimeouts persister, TransportTransactionMode transportTransactionMode)
        {
            this.dispatcher = dispatcher;
            this.persister = persister;
            dispatchConsistency = GetDispatchConsistency(transportTransactionMode);
        }

        protected override async Task Terminate(IIncomingPhysicalMessageContext context)
        {
            var message = context.Message;
            var timeoutId = message.Headers["Timeout.Id"];

            var timeoutData = await persister.Peek(timeoutId, context.Extensions).ConfigureAwait(false);

            if (timeoutData == null)
            {
                return;
            }

            var sendOptions = new DispatchOptions(new UnicastAddressTag(timeoutData.Destination), dispatchConsistency);

            timeoutData.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            timeoutData.Headers["NServiceBus.RelatedToTimeoutId"] = timeoutData.Id;

            await dispatcher.Dispatch(new[] { new TransportOperation(new OutgoingMessage(message.MessageId, timeoutData.Headers, timeoutData.State), sendOptions) }, context.Extensions).ConfigureAwait(false);

            var timeoutRemoved = await persister.TryRemove(timeoutId, context.Extensions).ConfigureAwait(false);
            if (!timeoutRemoved)
            {
                // timeout was concurrently removed between Peek and TryRemove. Throw an exception to rollback the dispatched message if possible.
                throw new Exception($"timeout '{timeoutId}' was concurrently processed.");
            }
        }

        static DispatchConsistency GetDispatchConsistency(TransportTransactionMode transportTransactionMode)
        {
            // dispatch message isolated from existing transactions when not using DTC to avoid loosing timeout data when the transaction commit fails.
            return transportTransactionMode == TransportTransactionMode.TransactionScope
                ? DispatchConsistency.Default
                : DispatchConsistency.Isolated;
        }

        IDispatchMessages dispatcher;
        IPersistTimeouts persister;
        readonly DispatchConsistency dispatchConsistency;
    }
}