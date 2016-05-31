namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    ///
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        ///
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        ///
        /// </summary>
        public bool IsAllowedToPerformRecoveryActions { get; }

        /// <summary>
        ///
        /// </summary>
        public ErrorContext(Exception exception, bool isAllowedToPerformRecoveryActions)
        {
            Exception = exception;
            IsAllowedToPerformRecoveryActions = isAllowedToPerformRecoveryActions;
        }
    }
}