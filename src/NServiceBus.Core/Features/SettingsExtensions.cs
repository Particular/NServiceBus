#nullable enable

namespace NServiceBus.Features;

using System;
using Settings;

/// <summary>
/// Feature related extensions to the settings.
/// </summary>
public static partial class SettingsExtensions
{
    /// <summary>
    /// Enables the given feature.
    /// </summary>
    /// <remarks>Enabling features is intended to be used for downstream components that only have access to settings. Features that need to enable other features should use <see cref="Feature.EnableByDefault{T}"/>.</remarks>
    /// <typeparam name="T">The feature to enable.</typeparam>
    public static void EnableFeature<T>(this SettingsHolder settings) where T : Feature
        => settings.Get<FeatureComponent.Settings>().EnableFeature<T>();

    /// <summary>
    /// Returns if a given feature has been activated in this endpoint.
    /// </summary>
    public static bool IsFeatureActive<T>(this IReadOnlySettings settings) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Get<FeatureComponent.Settings>().IsFeature<T>(FeatureState.Active);
    }

    /// <summary>
    /// Returns if a given feature has been enabled in this endpoint.
    /// </summary>
    public static bool IsFeatureEnabled<T>(this IReadOnlySettings settings) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Get<FeatureComponent.Settings>().IsFeature<T>(FeatureState.Enabled);
    }
}