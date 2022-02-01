namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// Abstraction representing any recoverability action.
    /// </summary>
    public abstract class RecoverabilityAction
    {
        /// <summary>
        /// Initializes a new instance of a recoverability action.
        /// </summary>
        protected internal RecoverabilityAction()
        {
        }

        /// <summary>
        /// Creates an immediate retry recoverability action.
        /// </summary>
        /// <returns>Immediate retry action.</returns>
        public static ImmediateRetry ImmediateRetry()
        {
            return CachedImmediateRetry;
        }

        /// <summary>
        /// Executes the recoverability action.
        /// </summary>
        public abstract IEnumerable<TransportOperation> Execute(
            ErrorContext errorContext,
            IDictionary<string, string> metadata);

        /// <summary>
        /// Creates a new delayed retry recoverability action.
        /// </summary>
        /// <param name="timeSpan">Delivery delay.</param>
        /// <returns>Delayed retry action.</returns>
        public static DelayedRetry DelayedRetry(TimeSpan timeSpan)
        {
            Guard.AgainstNegative(nameof(timeSpan), timeSpan);

            return new DelayedRetry(timeSpan);
        }

        /// <summary>
        /// Creates a move to error recoverability action.
        /// </summary>
        /// <param name="errorQueue">The address of the error queue.</param>
        /// <returns>Move to error action.</returns>
        public static MoveToError MoveToError(string errorQueue)
        {
            Guard.AgainstNullAndEmpty(nameof(errorQueue), errorQueue);
            return new MoveToError(errorQueue);
        }

        /// <summary>
        /// Creates a discard recoverability action.
        /// </summary>
        /// <param name="reason">The reason why the message was discarded.</param>
        /// <returns>Discard action.</returns>
        public static Discard Discard(string reason)
        {
            Guard.AgainstNullAndEmpty(nameof(reason), reason);
            return new Discard(reason);
        }

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public abstract ErrorHandleResult ErrorHandleResult { get; }

        static ImmediateRetry CachedImmediateRetry = new ImmediateRetry();
    }
}