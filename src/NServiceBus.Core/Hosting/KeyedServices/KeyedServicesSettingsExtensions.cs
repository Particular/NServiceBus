#nullable enable

namespace NServiceBus.Configuration.AdvancedExtensibility;

using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Settings;

/// <summary>
/// Advanced extensibility entry point for adapter-layer hosts that need to pre-create the keyed
/// service collection adapter used by a keyed NServiceBus endpoint.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class KeyedServicesSettingsExtensions
{
    /// <summary>
    /// Returns the keyed service collection adapter stored on <paramref name="settings"/>, creating and
    /// storing a new one that wraps <paramref name="services"/> under <paramref name="serviceKey"/> on
    /// the first call. Subsequent calls return the existing adapter.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection GetOrCreateKeyedServiceCollection(this SettingsHolder settings, IServiceCollection services, object serviceKey)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceKey);

        if (settings.GetOrDefault<KeyedServiceCollectionAdapter>() is { } existing)
        {
            return existing;
        }

        var adapter = new KeyedServiceCollectionAdapter(services, serviceKey);
        settings.Set(adapter);
        return adapter;
    }
}