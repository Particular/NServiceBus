#nullable enable

namespace NServiceBus;

using System;
using Features;

/// <summary>
/// Extension methods declarations.
/// </summary>
public static class EndpointConfigurationExtensions
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
    /// Enables the given feature.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="featureType">The feature to enable.</param>
    public static void EnableFeature(this EndpointConfiguration config, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(featureType);

        config.Settings.EnableFeature(featureType);
    }

    /// <summary>
    /// Disables the given feature.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    public static void DisableFeature<T>(this EndpointConfiguration config) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Settings.DisableFeature<T>();
    }

    /// <summary>
    /// Enables the given feature.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    /// <param name="featureType">The feature to disable.</param>
    public static void DisableFeature(this EndpointConfiguration config, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(featureType);

        config.Settings.DisableFeature(featureType);
    }
}