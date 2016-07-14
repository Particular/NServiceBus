namespace NServiceBus
{
    using System;
    using Logging;
    using Transport;

    class DefaultRecoverabilityPolicy
    {
        public static RecoverabilityAction Invoke(RecoverabilityConfig config, ErrorContext errorContext)
        {
            if (config.Immediate.MaxNumberOfRetries > 0)
            {
                if (errorContext.NumberOfImmediateDeliveryAttempts <= config.Immediate.MaxNumberOfRetries)
                {
                    return RecoverabilityAction.ImmediateRetry();
                }

                Logger.InfoFormat("Giving up First Level Retries for message '{0}'.", errorContext.Message.MessageId);
            }

            TimeSpan delay;
            if (TryGetDelay(errorContext.Message, errorContext.NumberOfDelayedDeliveryAttempts, config.Delayed, out delay))
            {
                return RecoverabilityAction.DelayedRetry(delay);
            }

            Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", errorContext.Message.MessageId);
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

        public static int DefaultNumberOfRetries = 3;
        public static TimeSpan DefaultTimeIncrease = TimeSpan.FromSeconds(10);

        static ILog Logger = LogManager.GetLogger<DefaultRecoverabilityPolicy>();
    }
}