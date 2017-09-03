namespace NServiceBus
{
    using System;
    using Transport;

    /// <summary>
    /// The default recoverability policy.
    /// </summary>
    public static class DefaultRecoverabilityPolicy
    {
        /// <summary>
        /// Invokes the default recovery policy.
        /// </summary>
        /// <param name="config">The recoverability configuration.</param>
        /// <param name="errorContext">The error context.</param>
        /// <returns>The recoverability action.</returns>
        public static RecoverabilityAction Invoke(RecoverabilityConfig config, ErrorContext errorContext)
        {
            Guard.AgainstNull(nameof(errorContext), errorContext);
            Guard.AgainstNull(nameof(config), config);
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var unrecoverableExceptionType in config.Failed.UnrecoverableExceptionTypes)
            {
                if (unrecoverableExceptionType.IsInstanceOfType(errorContext.Exception))
                {
                    return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
                }
            }

            if (errorContext.ImmediateProcessingFailures <= config.Immediate.MaxNumberOfRetries)
            {
                return RecoverabilityAction.ImmediateRetry();
            }

            if (TryGetDelay(errorContext.Message, errorContext.DelayedDeliveriesPerformed, config.Delayed, out var delay))
            {
                return RecoverabilityAction.DelayedRetry(delay);
            }

            return RecoverabilityAction.MoveToError(config.Failed.ErrorQueue);
        }

        static bool TryGetDelay(IncomingMessage message, int delayedDeliveriesPerformed, DelayedConfig config, out TimeSpan delay)
        {
            delay = TimeSpan.MinValue;

            if (config.MaxNumberOfRetries == 0)
            {
                return false;
            }

            if (delayedDeliveriesPerformed >= config.MaxNumberOfRetries)
            {
                return false;
            }

            if (HasReachedMaxTime(message))
            {
                return false;
            }

            delay = TimeSpan.FromTicks(config.TimeIncrease.Ticks*(delayedDeliveriesPerformed + 1));

            return true;
        }

        static bool HasReachedMaxTime(IncomingMessage message)
        {
            if (!message.Headers.TryGetValue(Headers.DelayedRetriesTimestamp, out var timestampHeader))
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

                var now = DateTime.UtcNow;
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

    }
}