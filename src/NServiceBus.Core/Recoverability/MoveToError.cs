namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Logging;
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

            Logger.Error($"Moving message '{message.MessageId}' to the error queue '{ErrorQueue}' because processing failed due to an exception:", errorContext.Exception);

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            foreach (var faultMetadata in metadata)
            {
                headers[faultMetadata.Key] = faultMetadata.Value;
            }

            yield return new TransportOperation(outgoingMessage, new UnicastAddressTag(ErrorQueue));
        }

        static ILog Logger = LogManager.GetLogger<MoveToError>();
    }
}