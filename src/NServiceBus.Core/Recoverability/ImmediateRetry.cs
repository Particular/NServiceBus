namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Logging;
    using NServiceBus.Transport;
    using Pipeline;

    /// <summary>
    /// Indicates recoverability is required to immediately retry the current message.
    /// </summary>
    public class ImmediateRetry : RecoverabilityAction
    {
        /// <summary>
        /// Creates an immediate retry action.
        /// </summary>
        protected internal ImmediateRetry()
        {
        }

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.RetryRequired;

        /// <inheritdoc />
        public override IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context)
        {
            var errorContext = context.ErrorContext;

            Logger.Info($"Immediate Retry is going to retry message '{errorContext.Message.MessageId}' because of an exception:", errorContext.Exception);
            if (context is IRecoverabilityActionContextNotifications notifications)
            {
                notifications.Add(new MessageToBeRetried(
                    attempt: errorContext.ImmediateProcessingFailures - 1,
                    delay: TimeSpan.Zero,
                    immediateRetry: true,
                    errorContext: errorContext));
            }
            return Array.Empty<IRoutingContext>();
        }

        static readonly ILog Logger = LogManager.GetLogger<ImmediateRetry>();
    }
}