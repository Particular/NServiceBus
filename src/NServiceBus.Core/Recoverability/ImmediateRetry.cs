namespace NServiceBus
{
    using System.Collections.Generic;
    using NServiceBus.Logging;
    using NServiceBus.Transport;

    /// <summary>
    /// Indicates recoverability is required to immediately retry the current message.
    /// </summary>
    public class ImmediateRetry : RecoverabilityAction
    {
        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.RetryRequired;

        /// <summary>
        /// Executes the recoverability action.
        /// </summary>
        public override IEnumerable<TransportOperation> GetTransportOperations(
            ErrorContext errorContext,
            IDictionary<string, string> metadata)
        {
            Logger.Info($"Immediate Retry is going to retry message '{errorContext.Message.MessageId}' because of an exception:", errorContext.Exception);
            yield break;
        }

        static ILog Logger = LogManager.GetLogger<ImmediateRetry>();
    }
}