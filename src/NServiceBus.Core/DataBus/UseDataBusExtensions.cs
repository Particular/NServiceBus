namespace NServiceBus
{
    using System;
    using NServiceBus.DataBus;

    /// <summary>
    /// Extension methods to configure data bus
    /// </summary>
    public static class UseDataBusExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given data bus definition.
        /// </summary>
        public static DataBusExtentions<T> UseDataBus<T>(this BusConfiguration config) where T : DataBusDefinition, new()
        {
            Guard.AgainstNull(config, "config");
            var type = typeof(DataBusExtentions<>).MakeGenericType(typeof(T));
            var extension = (DataBusExtentions<T>)Activator.CreateInstance(type, config.Settings);
            var definition = (DataBusDefinition)Activator.CreateInstance(typeof(T));

            config.Settings.Set("SelectedDataBus", definition);

            config.EnableFeature<Features.DataBus>();

            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use a custom <see cref="IDataBus"/> implementation.
        /// </summary>
        public static DataBusExtentions UseDataBus(this BusConfiguration config, Type dataBusType)
        {
            Guard.AgainstNull(config, "config");
            Guard.AgainstNull(dataBusType, "dataBusType");

            if (!typeof(IDataBus).IsAssignableFrom(dataBusType))
            {
                throw new ArgumentException("The type needs to implement IDataBus.", "dataBusType");
            }

            config.Settings.Set("SelectedDataBus", new CustomDataBus());
            config.Settings.Set("CustomDataBusType", dataBusType);

            config.EnableFeature<Features.DataBus>();

            return new DataBusExtentions(config.Settings);
        }
    }
}
