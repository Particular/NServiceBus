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
    /// Creates a keyed service collection adapter that wraps <paramref name="services"/> under
    /// <paramref name="serviceKey"/>, stores it on <paramref name="settings"/>, and returns it.
    /// <see cref="ServiceCollectionExtensions.AddNServiceBusEndpoint"/> will pick up the preloaded
    /// adapter instead of creating a new one, allowing the host to share a single adapter with user
    /// configuration code that runs before the endpoint is registered.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddKeyedServiceCollection(this SettingsHolder settings, IServiceCollection services, object serviceKey)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(serviceKey);

        var adapter = new KeyedServiceCollectionAdapter(services, serviceKey);
        settings.Set(adapter);
        return adapter;
    }
}