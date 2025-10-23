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
    /// TODO Obsolete?
    /// Returns if a given feature has been activated in this endpoint.
    /// </summary>
    public static bool IsFeatureActive(this IReadOnlySettings settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(featureType);
        return settings.Get<FeatureComponent.Settings>().IsFeature(featureType, FeatureState.Active);
    }

    /// <summary>
    /// Returns if a given feature has been activated in this endpoint.
    /// </summary>
    public static bool IsFeatureActive<T>(this IReadOnlySettings settings) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Get<FeatureComponent.Settings>().IsFeature<T>(FeatureState.Active);
    }

    /// <summary>
    /// TODO Obsolete?
    /// Returns if a given feature has been enabled in this endpoint.
    /// </summary>
    public static bool IsFeatureEnabled(this IReadOnlySettings settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(featureType);
        return settings.Get<FeatureComponent.Settings>().IsFeature(featureType, FeatureState.Enabled);
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