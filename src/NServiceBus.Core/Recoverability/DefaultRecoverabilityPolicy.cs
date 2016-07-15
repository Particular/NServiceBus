namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// The default recoverability policy.
    /// </summary>
    public class DefaultRecoverabilityPolicy
    {
        /// <summary>
        /// Invokes the default recovery policy.
        /// </summary>
        /// <param name="config">The recoverability configuration.</param>
        /// <param name="errorContext">The error context.</param>
        /// <returns>The recoverability action.</returns>
        public static RecoverabilityAction Invoke(RecoverabilityConfig config, ErrorContext errorContext)
        {
            if (errorContext.Exception is MessageDeserializationException)
            {
                return RecoverabilityAction.MoveToError();
            }

            if (errorContext.NumberOfImmediateDeliveryAttempts <= config.Immediate.MaxNumberOfRetries)
            {
                return RecoverabilityAction.ImmediateRetry();
            }

            TimeSpan delay;
            if (TryGetDelay(errorContext.Message, errorContext.NumberOfDelayedDeliveryAttempts, config.Delayed, out delay))
            {
                return RecoverabilityAction.DelayedRetry(delay);
            }

            return RecoverabilityAction.MoveToError();
        }

        static bool TryGetDelay(IncomingMessage message, int currentDelayedDeliveryAttempts, DelayedConfig config, out TimeSpan delay)
        {
            delay = TimeSpan.MinValue;

            if (config.MaxNumberOfRetries == 0)
            {
                return false;
            }

            if (currentDelayedDeliveryAttempts > config.MaxNumberOfRetries)
            {
                return false;
            }

            if (HasReachedMaxTime(message))
            {
                return false;
            }

            delay = TimeSpan.FromTicks(config.TimeIncrease.Ticks*currentDelayedDeliveryAttempts);

            return true;
        }

        static bool HasReachedMaxTime(IncomingMessage message)
        {
            string timestampHeader;

            if (!message.Headers.TryGetValue(Headers.RetriesTimestamp, out timestampHeader))
            {
                return false;
            }

            if (string.IsNullOrEmpty(timestampHeader))
            {
                return false;
            }

            try
            {
                var handledAt = DateTimeExtensions.ToUtcDateTime(timestampHeader);

                var now = CurrentUtcTimeProvider();
                if (now > handledAt.AddDays(1))
                {
                    return true;
                }
            }
                // ReSharper disable once EmptyGeneralCatchClause
                // this code won't usually throw but in case a user has decided to hack a message/headers and for some bizarre reason
                // they changed the date and that parse fails, we want to make sure that doesn't prevent the message from being
                // forwarded to the error queue.
            catch (Exception)
            {
            }

            return false;
        }

        internal static Func<DateTime> CurrentUtcTimeProvider = () => DateTime.UtcNow;
    }
}