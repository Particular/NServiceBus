#nullable enable

namespace NServiceBus;

using System;
using Features;

/// <summary>
/// Extension methods declarations.
/// </summary>
public static partial class EndpointConfigurationExtensions
{
    /// <summary>
    /// Enables the given feature.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    public static void EnableFeature<T>(this EndpointConfiguration config) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Settings.EnableFeature<T>();
    }

    /// <summary>
    /// Disables the given feature.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    public static void DisableFeature<T>(this EndpointConfiguration config) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Settings.Get<FeatureComponent.Settings>().DisableFeature<T>();
    }
}