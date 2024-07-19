namespace NServiceBus;

using System;
using System.Collections.Generic;
using Configuration.AdvancedExtensibility;
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
    public static DataBusExtensions<TDataBusDefinition> UseDataBus<TDataBusDefinition, TDataBusSerializer>(this EndpointConfiguration config)
        where TDataBusDefinition : DataBusDefinition, new()
        where TDataBusSerializer : IDataBusSerializer, new()
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.UseDataBus<TDataBusDefinition>(new TDataBusSerializer());
    }

    /// <summary>
    /// Configures NServiceBus to use the given data bus definition.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="dataBusSerializer">The <see cref="IDataBusSerializer" /> instance to use.</param>
    public static DataBusExtensions<TDataBusDefinition> UseDataBus<TDataBusDefinition>(this EndpointConfiguration config, IDataBusSerializer dataBusSerializer)
        where TDataBusDefinition : DataBusDefinition, new()
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(dataBusSerializer);

        var dataBusExtensionType = typeof(DataBusExtensions<>).MakeGenericType(typeof(TDataBusDefinition));
        var dataBusExtension = (DataBusExtensions<TDataBusDefinition>)Activator.CreateInstance(dataBusExtensionType, config.GetSettings());
        var dataBusDefinition = new TDataBusDefinition();

        EnableDataBus(config, dataBusDefinition, dataBusSerializer);

        return dataBusExtension;
    }

    /// <summary>
    /// Configures NServiceBus to use a custom <see cref="IDataBus" /> implementation.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="dataBusFactory">The factory to create the custom <see cref="IDataBus" /> to use.</param>
    /// <param name="dataBusSerializer">The <see cref="IDataBusSerializer" /> instance to use.</param>
    public static DataBusExtensions UseDataBus(this EndpointConfiguration config, Func<IServiceProvider, IDataBus> dataBusFactory, IDataBusSerializer dataBusSerializer)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(dataBusFactory);
        ArgumentNullException.ThrowIfNull(dataBusSerializer);

        EnableDataBus(config, new CustomDataBus(dataBusFactory), dataBusSerializer);

        return new DataBusExtensions(config.GetSettings());
    }

    static void EnableDataBus(EndpointConfiguration config, DataBusDefinition selectedDataBus, IDataBusSerializer dataBusSerializer)
    {
        config.GetSettings().Set(Features.DataBus.SelectedDataBusKey, selectedDataBus);
        config.GetSettings().Set(Features.DataBus.DataBusSerializerKey, dataBusSerializer);
        config.GetSettings().Set(Features.DataBus.AdditionalDataBusDeserializersKey, new List<IDataBusSerializer>());

        if (!config.GetSettings().HasSetting(Features.DataBus.DataBusConventionsKey))
        {
            config.GetSettings().Set(Features.DataBus.DataBusConventionsKey, new DataBusConventions());
        }

        config.EnableFeature<Features.DataBus>();
    }
}