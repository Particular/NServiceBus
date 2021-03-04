namespace NServiceBus.Transport
{
    using System;

    /// <summary>
    /// Provides extensions for <see cref="ErrorHandleResult"/>.
    /// </summary>
    public static class ErrorHandleResultExtensions
    {
        /// <summary>
        /// Converts an instance of <see cref="ErrorHandleResult"/> to instance of <see cref="ReceiveResult"/>.
        /// </summary>
        public static ReceiveResult ToReceiveResult(this ErrorHandleResult errorHandleResult)
        {
            switch (errorHandleResult)
            {
                case ErrorHandleResult.Discarded:
                    return ReceiveResult.Discarded;
                case ErrorHandleResult.MovedToErrorQueue:
                    return ReceiveResult.MovedToErrorQueue;
                case ErrorHandleResult.QueuedForDelayedRetry:
                    return ReceiveResult.QueuedForDelayedRetry;
                case ErrorHandleResult.RetryRequired:
                default:
                    throw new ArgumentOutOfRangeException(nameof(errorHandleResult));
            }
        }
    }
}
