﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using DataBus;

    /// <summary>
    /// Extension methods to configure data bus.
    /// </summary>
    public static partial class UseDataBusExtensions
    {
        /// <summary>
        /// Configures NServiceBus to use the given data bus definition.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        public static DataBusExtensions<TDataBus> UseDataBus<TDataBus, TDataBusSerializer>(this EndpointConfiguration config)
            where TDataBus : DataBusDefinition, new()
            where TDataBusSerializer : IDataBusSerializer, new()
        {
            Guard.AgainstNull(nameof(config), config);

            var dataBusSerializer = (IDataBusSerializer)Activator.CreateInstance(typeof(TDataBusSerializer));

            return config.UseDataBus<TDataBus>(dataBusSerializer);
        }

        /// <summary>
        /// Configures NServiceBus to use the given data bus definition.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="dataBusSerializer">The <see cref="IDataBusSerializer" /> instance to use.</param>
        public static DataBusExtensions<TDataBus> UseDataBus<TDataBus>(this EndpointConfiguration config, IDataBusSerializer dataBusSerializer)
            where TDataBus : DataBusDefinition, new()
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(dataBusSerializer), dataBusSerializer);

            var dataBusType = typeof(DataBusExtensions<>).MakeGenericType(typeof(TDataBus));
            var dataBusExtension = (DataBusExtensions<TDataBus>)Activator.CreateInstance(dataBusType, config.Settings);
            var dataBusDefinition = (DataBusDefinition)Activator.CreateInstance(typeof(TDataBus));

            EnableDataBus(config, dataBusDefinition, dataBusSerializer);

            return dataBusExtension;
        }

        /// <summary>
        /// Configures NServiceBus to use a custom <see cref="IDataBus" /> implementation.
        /// </summary>
        /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
        /// <param name="dataBusType">The <see cref="IDataBus" /> <see cref="Type" /> to use.</param>
        /// <param name="dataBusSerializer">The <see cref="IDataBusSerializer" /> instance to use.</param>
        public static DataBusExtensions UseDataBus(this EndpointConfiguration config, Type dataBusType, IDataBusSerializer dataBusSerializer)
        {
            Guard.AgainstNull(nameof(config), config);
            Guard.AgainstNull(nameof(dataBusType), dataBusType);
            Guard.AgainstNull(nameof(dataBusSerializer), dataBusSerializer);

            if (!typeof(IDataBus).IsAssignableFrom(dataBusType))
            {
                throw new ArgumentException("The data bus type needs to implement IDataBus.", nameof(dataBusType));
            }

            EnableDataBus(config, new CustomDataBus(dataBusType), dataBusSerializer);

            return new DataBusExtensions(config.Settings);
        }

        static void EnableDataBus(EndpointConfiguration config, DataBusDefinition selectedDataBus, IDataBusSerializer dataBusSerializer)
        {
            config.Settings.Set(Features.DataBus.SelectedDataBusKey, selectedDataBus);
            config.Settings.Set(Features.DataBus.DataBusSerializerKey, dataBusSerializer);
            config.Settings.Set(Features.DataBus.AdditionalDataBusDeserializersKey, new List<IDataBusSerializer>());

            config.EnableFeature<Features.DataBus>();
        }
    }
}