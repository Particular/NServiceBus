namespace NServiceBus.DataBus;

using System;
using System.Collections.Generic;
using System.Linq;
using Configuration.AdvancedExtensibility;
using Settings;

/// <summary>
/// This class provides implementers of databus with an extension mechanism for custom settings via extension methods.
/// </summary>
/// <typeparam name="T">The databus definition eg <see cref="FileShareDataBus" />.</typeparam>
public class DataBusExtensions<T> : DataBusExtensions where T : DataBusDefinition
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public DataBusExtensions(SettingsHolder settings)
        : base(settings)
    {
    }
}

/// <summary>
/// This class provides implementers of databus with an extension mechanism for custom settings via extension methods.
/// </summary>
public class DataBusExtensions : ExposeSettings
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public DataBusExtensions(SettingsHolder settings)
        : base(settings)
    {
    }

    /// <summary>
    /// Configures additional deserializers to be considered when processing data bus properties. Can be called multiple times.
    /// </summary>
    public DataBusExtensions AddDeserializer<TSerializer>() where TSerializer : IDataBusSerializer, new()
    {
        var serializer = (TSerializer)Activator.CreateInstance(typeof(TSerializer));

        return AddDeserializer(serializer);
    }

    /// <summary>
    /// Configures additional deserializers to be considered when processing data bus properties. Can be called multiple times.
    /// </summary>
    public DataBusExtensions AddDeserializer<TSerializer>(TSerializer serializer) where TSerializer : IDataBusSerializer
    {
        ArgumentNullException.ThrowIfNull(serializer);

        var deserializers = this.GetSettings().Get<List<IDataBusSerializer>>(Features.DataBus.AdditionalDataBusDeserializersKey);

        if (deserializers.Any(d => d.ContentType == serializer.ContentType))
        {
            throw new ArgumentException($"Deserializer for content type  '{serializer.ContentType}' is already registered.");
        }

        var mainSerializer = this.GetSettings().Get<IDataBusSerializer>(Features.DataBus.DataBusSerializerKey);

        if (mainSerializer.ContentType == serializer.ContentType)
        {
            throw new ArgumentException($"Main serializer already handles content type '{serializer.ContentType}'.");
        }

        deserializers.Add(serializer);

        return this;
    }
}