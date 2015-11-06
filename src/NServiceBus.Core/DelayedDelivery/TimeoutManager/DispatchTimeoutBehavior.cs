namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Routing;
    using Timeout.Core;
    using Transports;

    class DispatchTimeoutBehavior : SatelliteBehavior
    {
        public DispatchTimeoutBehavior(IDispatchMessages dispatcher, IPersistTimeouts persister, TransactionSupport transactionSupport)
        {
            this.dispatcher = dispatcher;
            this.persister = persister;
            this.dispatchConsistency = GetDispatchConsistency(transactionSupport);
        }

        protected override async Task Terminate(PhysicalMessageProcessingContext context)
        {
            var message = context.Message;
            var timeoutId = message.Headers["Timeout.Id"];

            var timeoutData = await persister.Peek(timeoutId, context).ConfigureAwait(false);

            if (timeoutData == null)
            {
                return;
            }

            var sendOptions = new DispatchOptions(new UnicastAddressTag(timeoutData.Destination), dispatchConsistency);

            timeoutData.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            timeoutData.Headers["NServiceBus.RelatedToTimeoutId"] = timeoutData.Id;

            await dispatcher.Dispatch(new[] { new TransportOperation(new OutgoingMessage(message.MessageId, timeoutData.Headers, timeoutData.State), sendOptions) }, context).ConfigureAwait(false);

            var timeoutRemoved = await persister.TryRemove(timeoutId, context).ConfigureAwait(false);
            if (!timeoutRemoved)
            {
                // timeout was concurrently removed between Peek and TryRemove. Throw an exception to rollback the dispatched message if possible.
                throw new Exception($"timeout '{timeoutId}' was concurrently processed.");
            }
        }

        static DispatchConsistency GetDispatchConsistency(TransactionSupport transactionSupport)
        {
            // dispatch message isolated from existing transactions when not using DTC to avoid loosing timeout data when the transaction commit fails.
            return transactionSupport == TransactionSupport.Distributed
                ? DispatchConsistency.Default
                : DispatchConsistency.Isolated;
        }

        IDispatchMessages dispatcher;
        IPersistTimeouts persister;
        readonly DispatchConsistency dispatchConsistency;
    }
}