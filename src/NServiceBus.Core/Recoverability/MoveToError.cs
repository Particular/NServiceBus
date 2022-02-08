namespace NServiceBus
{
    using System.Collections.Generic;
    using Logging;
    using Pipeline;
    using Recoverability;
    using Routing;
    using Transport;

    /// <summary>
    /// Indicates that recoverability is required to move the current message to the error queue.
    /// </summary>
    public class MoveToError : RecoverabilityAction
    {
        /// <summary>
        /// Creates the action with the target error queue.
        /// </summary>
        protected internal MoveToError(string errorQueue) => ErrorQueue = errorQueue;

        /// <summary>
        /// Defines the error queue where the message should be move to.
        /// </summary>
        public string ErrorQueue { get; }

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;

        /// <inheritdoc />
        public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
        {
            var errorContext = context.ErrorContext;
            var metadata = context.Metadata;
            var message = errorContext.Message;

            Logger.Error($"Moving message '{message.MessageId}' to the error queue '{ErrorQueue}' because processing failed due to an exception:", errorContext.Exception);

            if (context is IRecoverabilityActionContextNotifications notifications)
            {
                notifications.Add(new MessageFaulted(errorContext, ErrorQueue));
            }

            var outgoingMessage = new OutgoingMessage(message.MessageId, new Dictionary<string, string>(message.Headers), message.Body);

            var headers = outgoingMessage.Headers;
            headers.Remove(Headers.DelayedRetries);
            headers.Remove(Headers.ImmediateRetries);

            foreach (var faultMetadata in metadata)
            {
                headers[faultMetadata.Key] = faultMetadata.Value;
            }
            return new[]
            {
                context.CreateRoutingContext(outgoingMessage, new UnicastRoutingStrategy(ErrorQueue))
            };
        }

        static readonly ILog Logger = LogManager.GetLogger<MoveToError>();
    }
}