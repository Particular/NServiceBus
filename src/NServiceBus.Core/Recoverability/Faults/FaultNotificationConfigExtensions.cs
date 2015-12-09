namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Faults;
    using NServiceBus.Settings;

    /// <summary>
    /// Provides config options for the faults feature.
    /// </summary>
    public static class FaultNotificationConfigExtensions
    {

        /// <summary>
        /// Allows for customization of the faults.
        /// </summary>
        /// <param name="busConfiguration">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        public static FaultsSettings Faults(this BusConfiguration busConfiguration)
        {
            Guard.AgainstNull("busConfiguration", busConfiguration);
            return new FaultsSettings(busConfiguration);
        }

        internal static void AddNotifyOnFailedMessage(this SettingsHolder settings, Action<FailedMessage> action)
        {
            FailedMessageActions actions;
            if (!settings.TryGet(out actions))
            {
                actions = new FailedMessageActions();
                settings.Set<FailedMessageActions>(actions);
            }
            actions.Actions.Add(action);
        }

        class FailedMessageActions
        {
            internal List<Action<FailedMessage>> Actions = new List<Action<FailedMessage>>();
        }

        internal static IEnumerable<Action<FailedMessage>> GetFailedMessageActions(this ReadOnlySettings settings)
        {
            FailedMessageActions actions;
            if (settings.TryGet(out actions))
            {
                return actions.Actions;
            }
            return new List<Action<FailedMessage>>();
        }

    }
}