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
        public static DataBusExtensions<TDataBus> UseDataBus<TDataBus, TDataBusSerializer>(this EndpointConfiguration config) where TDataBus : DataBusDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);
            var type = typeof(DataBusExtensions<>).MakeGenericType(typeof(TDataBus));
            var extension = (DataBusExtensions<TDataBus>)Activator.CreateInstance(type, config.Settings);
            var definition = (DataBusDefinition)Activator.CreateInstance(typeof(TDataBus));
            config.Settings.Set(Features.DataBus.SelectedDataBusKey, definition);
            config.Settings.Set(Features.DataBus.DataBusSerializerTypeKey, typeof(TDataBusSerializer));

            config.EnableFeature<Features.DataBus>();

            return extension;
        }

        /// <summary>
        /// Configures NServiceBus to use a custom <see cref="IDataBus" /> implementation.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="dataBusType">The <see cref="IDataBus" /> <see cref="Type" /> to use.</param>
        /// <param name="dataBusSerializerType">The data bus serializer <see cref="Type" /> to use.</param>
        public static DataBusExtensions UseDataBus(this EndpointConfiguration config, Type dataBusType, Type dataBusSerializerType)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(dataBusType), dataBusType);
            Guard.AgainstNull(nameof(dataBusSerializerType), dataBusSerializerType);

            if (!typeof(IDataBus).IsAssignableFrom(dataBusType))
            {
                throw new ArgumentException("The type needs to implement IDataBus.", nameof(dataBusType));
            }

            config.Settings.Set(Features.DataBus.SelectedDataBusKey, new CustomDataBus());
            config.Settings.Set(Features.DataBus.CustomDataBusTypeKey, dataBusType);
            config.Settings.Set(Features.DataBus.DataBusSerializerTypeKey, dataBusSerializerType);

            config.EnableFeature<Features.DataBus>();

            return new DataBusExtensions(config.Settings);
        }

        /// <summary>
        /// Configures NServiceBus to use the given data bus definition.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        [ObsoleteEx(
            Message = "Specifying data bus serializer is mandatory. Use the overload that acceptes a data bus serializer type.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static DataBusExtensions<T> UseDataBus<T>(this EndpointConfiguration config) where T : DataBusDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);

            return config.UseDataBus<T, string>(); //todo: pass binary serializer type
        }

        /// <summary>
        /// Configures NServiceBus to use a custom <see cref="IDataBus" /> implementation.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="dataBusType">The <see cref="IDataBus" /> <see cref="Type" /> to use.</param>
        [ObsoleteEx(
            Message = "Specifying data bus serializer is mandatory. Use the overload that acceptes a data bus serializer type.",
            TreatAsErrorFromVersion = "9.0",
            RemoveInVersion = "10.0")]
        public static DataBusExtensions UseDataBus(this EndpointConfiguration config, Type dataBusType)
        {
            Guard.AgainstNull(nameof(config), config);

            return config.UseDataBus(dataBusType, typeof(string)); //todo: pass binary serializer type
        }
    }
}