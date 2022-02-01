namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Logging;
    using NServiceBus.Transport;

    /// <summary>
    /// Indicates recoverability is required to discard/ignore the current message.
    /// </summary>
    public sealed class Discard : RecoverabilityAction
    {
        internal Discard(string reason)
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

        /// <summary>
        /// Executes the recoverability action.
        /// </summary>
        public override IEnumerable<TransportOperation> Execute(
            ErrorContext errorContext,
            IDictionary<string, string> metadata)
        {
            Logger.Info($"Discarding message with id '{errorContext.Message.MessageId}'. Reason: {Reason}", errorContext.Exception);
            yield break;
        }

        static ILog Logger = LogManager.GetLogger<Discard>();
    }
}