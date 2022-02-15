namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Pipeline;
    using Transport;

    /// <summary>
    /// Indicates recoverability is required to discard/ignore the current message.
    /// </summary>
    public class Discard : RecoverabilityAction
    {
        /// <summary>
        /// Creates the action with the stated reason.
        /// </summary>
        public Discard(string reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// The reason why a message was discarded.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// How to handle the message from a transport perspective.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;

        /// <inheritdoc />
        public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
        {
            Logger.Info($"Discarding message with id '{context.FailedMessage.MessageId}'. Reason: {Reason}", context.Exception);
            return Array.Empty<IRoutingContext>();
        }

        static readonly ILog Logger = LogManager.GetLogger<Discard>();
    }
}