namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.Routing;
    using NServiceBus.Transport;

    /// <summary>
    /// Indicates that recoverability is required to move the current message to the error queue.
    /// </summary>
    public sealed class MoveToError : RecoverabilityAction
    {
        internal MoveToError(string errorQueue)
        {
            ErrorQueue = errorQueue;
        }

        /// <summary>
        /// Defines the error queue where the message should be move to.
        /// </summary>
        public string ErrorQueue { get; }

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
            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            headers[FaultsHeaderKeys.FailedQ] = errorContext.ReceiveAddress;

            ExceptionHeaderHelper.SetExceptionHeaders(headers, errorContext.Exception);

            //foreach (var faultMetadata in staticFaultMetadata)
            //{
            //    headers[faultMetadata.Key] = faultMetadata.Value;
            //}

            //headerCustomizations(headers);

            yield return new TransportOperation(outgoingMessage, new UnicastAddressTag(ErrorQueue));
        }
    }
}