namespace NServiceBus
{
    using System;

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
        public static RecoverabilityAction ImmediateRetry()
        {
            return CachedImmediateRetry;
        }

        /// <summary>
        /// Creates a new delayed retry recoverability action.
        /// </summary>
        /// <param name="timeSpan">Delivery delay.</param>
        /// <returns>Delayed retry action.</returns>
        public static RecoverabilityAction DelayedRetry(TimeSpan timeSpan)
        {
            Guard.AgainstNegativeAndZero(nameof(timeSpan), timeSpan);

            return new DelayedRetry(timeSpan);
        }

        /// <summary>
        /// Creates a move to error recoverability action.
        /// </summary>
        /// <returns>Move to error action.</returns>
        public static RecoverabilityAction MoveToError()
        {
            return CachedMoveToError;
        }

        static RecoverabilityAction CachedImmediateRetry = new ImmediateRetry();

        static RecoverabilityAction CachedMoveToError = new MoveToError();
    }
}