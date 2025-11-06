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
    public static void EnableFeature<TFeature>(this EndpointConfiguration config) where TFeature : Feature, IFeatureFactory
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Settings.EnableFeature<TFeature>();
    }

    /// <summary>
    /// Disables the given feature.
    /// </summary>
    /// <param name="config">The <see cref="EndpointConfiguration" /> instance to apply the settings to.</param>
    public static void DisableFeature<TFeature>(this EndpointConfiguration config) where TFeature : Feature, IFeatureFactory
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Settings.DisableFeature<TFeature>();
    }
}