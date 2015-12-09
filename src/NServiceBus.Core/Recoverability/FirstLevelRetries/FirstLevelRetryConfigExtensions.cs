namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.SecondLevelRetries.Config;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides config options for the FLR feature.
    /// </summary>
    public static class FirstLevelRetryConfigExtensions
    {

        /// <summary>
        /// Allows for customization of the first level retries.
        /// </summary>
        /// <param name="busConfiguration">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static FirstLevelRetriesSettings FirstLevelRetries(this BusConfiguration busConfiguration)
        {
            Guard.AgainstNull("busConfiguration", busConfiguration);
            return new FirstLevelRetriesSettings(busConfiguration);
        }

        internal static void AddNotifyOnFirstLevelRetry(this SettingsHolder settings, Action<FirstLevelRetry> action)
        {
            FirstLevelRetryActions actions;
            if (!settings.TryGet(out actions))
            {
                actions = new FirstLevelRetryActions();
                settings.Set<FirstLevelRetryActions>(actions);
            }
            actions.Actions.Add(action);
        }

        class FirstLevelRetryActions
        {
            internal List<Action<FirstLevelRetry>> Actions = new List<Action<FirstLevelRetry>>();
        }

        internal static IEnumerable<Action<FirstLevelRetry>> GetFirstLevelRetryActions(this ReadOnlySettings settings)
        {
            FirstLevelRetryActions actions;
            if (settings.TryGet(out actions))
            {
                return actions.Actions;
            }
            return new List<Action<FirstLevelRetry>>();
        }

    }
}