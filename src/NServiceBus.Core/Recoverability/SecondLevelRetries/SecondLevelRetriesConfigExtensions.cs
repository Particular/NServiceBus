namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.Settings;
    using SecondLevelRetries.Config;

    /// <summary>
    /// Provides config options for the SLR feature.
    /// </summary>
    public static class SecondLevelRetriesConfigExtensions
    {
        /// <summary>
        /// Allows for customization of the second level retries.
        /// </summary>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static SecondLevelRetriesSettings SecondLevelRetries(this BusConfiguration config)
        {
            Guard.AgainstNull("config", config);
            return new SecondLevelRetriesSettings(config);
        }

        internal static void AddNotifyOnSecondLevelRetry(this SettingsHolder settings, Action<SecondLevelRetry> action)
        {
            SecondLevelRetryActions actions;
            if (!settings.TryGet(out actions))
            {
                actions = new SecondLevelRetryActions();
                settings.Set<SecondLevelRetryActions>(actions);
            }
            actions.Actions.Add(action);
        }

        class SecondLevelRetryActions
        {
            internal List<Action<SecondLevelRetry>> Actions = new List<Action<SecondLevelRetry>>();
        }

        internal static IEnumerable<Action<SecondLevelRetry>> GetSecondLevelRetryActions(this ReadOnlySettings settings)
        {
            SecondLevelRetryActions actions;
            if (settings.TryGet(out actions))
            {
                return actions.Actions;
            }
            return new List<Action<SecondLevelRetry>>();
        }
    }
}