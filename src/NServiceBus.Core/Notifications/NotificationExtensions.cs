namespace NServiceBus
{
    using NServiceBus.Settings;

    /// <summary>
    /// Adds extensions methods to get notified of bus events.
    /// </summary>
    public static class NotificationExtensions
    {

        /// <summary>
        /// Set a delegate that will be called when a message is moved to the error queue.
        /// </summary>
        public static void NotifyOnFailedMessage(this BusConfiguration busConfiguration, FailedMessageAction action)
        {
            Guard.AgainstNull(nameof(action), action);
            busConfiguration.Settings.Set<FailedMessageAction>(action);
        }

        internal static FailedMessageAction GetFailedMessageAction(this ReadOnlySettings settings)
        {
            FailedMessageAction action;
            if (settings.TryGet(out action))
            {
                return action;
            }
            return x=> {};
        }

        /// <summary>
        /// Set a delegate that will be called when a message fails a first level retry.
        /// </summary>
        public static void NotifyOnFirstLevelRetry(this BusConfiguration busConfiguration, FirstLevelRetryAction action)
        {
            Guard.AgainstNull(nameof(action), action);
            busConfiguration.Settings.Set<FirstLevelRetryAction>(action);
        }

        internal static FirstLevelRetryAction GetFirstLevelRetryAction(this ReadOnlySettings settings)
        {
            FirstLevelRetryAction action;
            if (settings.TryGet(out action))
            {
                return action;
            }
            return x => { };
        }

        /// <summary>
        /// Set a delegate that will be called when a message is sent to second level retires queue.
        /// </summary>
        public static void NotifyOnSecondLevelRetry(this BusConfiguration busConfiguration, SecondLevelRetryAction action)
        {
            Guard.AgainstNull(nameof(action), action);
            busConfiguration.Settings.Set<SecondLevelRetryAction>(action);
        }

        internal static SecondLevelRetryAction GetSecondLevelRetryAction(this ReadOnlySettings settings)
        {
            SecondLevelRetryAction action;
            if (settings.TryGet(out action))
            {
                return action;
            }
            return x => { };
        }
    }
}