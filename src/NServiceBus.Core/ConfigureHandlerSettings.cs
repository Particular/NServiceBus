namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// Extension methods for injecting props in <see cref="IHandleMessages{T}"/>.
    /// </summary>
    public static class ConfigureHandlerSettings
    {
        /// <summary>
        /// Initializes <see cref="IHandleMessages{T}"/> with the specified properties.
        /// </summary>
        /// <typeparam name="THandler">The <see cref="IHandleMessages{T}"/> type.</typeparam>
        /// <param name="config">The <see cref="BusConfiguration"/> instance to apply the settings to.</param>
        /// <param name="property">The property name to be injected.</param>
        /// <param name="value">The value to assign to the <paramref name="property"/>.</param>
        public static void InitializeHandlerProperty<THandler>(this BusConfiguration config, string property, object value)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNullAndEmpty(nameof(property), property);
            Guard.AgainstNull(nameof(value), value);
            List<Action<IConfigureComponents>> list;
            if (!config.Settings.TryGet("NServiceBus.HandlerProperties", out list))
            {
                list = new List<Action<IConfigureComponents>>();
                config.Settings.Set("NServiceBus.HandlerProperties", list);
            }

            list.Add(c=> c.ConfigureProperty<THandler>(property, value));
        }
    }
}