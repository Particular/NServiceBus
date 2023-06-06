namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Transport;
    using Pipeline;

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
        /// Specifies that messages matching this recoverability policy will be retried immediately.
        /// </summary>
        /// <returns>Immediate retry action.</returns>
        public static ImmediateRetry ImmediateRetry() => CachedImmediateRetry;

        /// <summary>
        /// Returns the routing contexts derived from the provided recoverability context.
        /// </summary>
        public abstract IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context);

        /// <summary>
        /// Specifies that messages matching this recoverability policy will be retried with a specified delay.
        /// </summary>
        /// <param name="timeSpan">Delivery delay.</param>
        /// <returns>Delayed retry action.</returns>
        public static DelayedRetry DelayedRetry(TimeSpan timeSpan)
        {
            Guard.ThrowIfNegative(timeSpan);

            return new DelayedRetry(timeSpan);
        }

        /// <summary>
        /// Specifies that messages matching this recoverability policy will not be retried and will be routed to the error queue.
        /// </summary>
        /// <param name="errorQueue">The address of the error queue.</param>
        /// <returns>Move to error action.</returns>
        public static MoveToError MoveToError(string errorQueue)
        {
            Guard.ThrowIfNullOrEmpty(errorQueue);
            return new MoveToError(errorQueue);
        }

        /// <summary>
        /// Specifies that messages matching this recoverability policy will be discarded as if they had never happened.
        /// The message will not be forwarded to the audit queue, which may be confusing when viewing audit data as
        /// the conversation will abruptly end without any information about the reason.
        /// </summary>
        /// <param name="reason">
        /// The reason why the message was discarded. This reason is communicated only in logs and is not transmitted
        /// through audit messages to the audit queue.
        /// </param>
        /// <returns>Discard action.</returns>
        public static Discard Discard(string reason)
        {
            Guard.ThrowIfNullOrEmpty(reason);
            return new Discard(reason);
        }

        /// <summary>
        /// The ErrorHandleResult that should be passed to the transport.
        /// </summary>
        public abstract ErrorHandleResult ErrorHandleResult { get; }

        static readonly ImmediateRetry CachedImmediateRetry = new ImmediateRetry();
    }
}