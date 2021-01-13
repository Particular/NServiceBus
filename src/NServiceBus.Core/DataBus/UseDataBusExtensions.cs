namespace NServiceBus
{
    using System;
    using DataBus;

    /// <summary>
    /// Extension methods to configure data bus.
    /// </summary>
    public static class UseDataBusExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given data bus definition.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static DataBusExtensions<T> UseDataBus<T>(this EndpointConfiguration config) where T : DataBusDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);
            var type = typeof(DataBusExtensions<>).MakeGenericType(typeof(T));
            var extension = (DataBusExtensions<T>)Activator.CreateInstance(type, config.Settings);
            var definition = (DataBusDefinition)Activator.CreateInstance(typeof(T));
            config.Settings.Set("SelectedDataBus", definition);

            config.EnableFeature<Features.DataBus>();

            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use a custom <see cref="IDataBus" /> implementation.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="dataBusType">The <see cref="IDataBus" /> <see cref="Type" /> to use.</param>
        public static DataBusExtensions UseDataBus(this EndpointConfiguration config, Type dataBusType)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(dataBusType), dataBusType);

            if (!typeof(IDataBus).IsAssignableFrom(dataBusType))
            {
                throw new ArgumentException("The type needs to implement IDataBus.", nameof(dataBusType));
            }

            config.Settings.Set("SelectedDataBus", new CustomDataBus());
            config.Settings.Set("CustomDataBusType", dataBusType);

            config.EnableFeature<Features.DataBus>();

            return new DataBusExtensions(config.Settings);
        }
    }
}