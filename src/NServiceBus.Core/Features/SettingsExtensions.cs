#nullable enable

namespace NServiceBus.Features;

using System;
using Settings;

/// <summary>
/// Feature related extensions to the settings.
/// </summary>
public static class SettingsExtensions
{
    /// <summary>
    /// TODO Obsolete?
    /// Marks the given feature as enabled by default.
    /// </summary>
    public static SettingsHolder EnableFeatureByDefault<T>(this SettingsHolder settings) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Get<FeatureComponent.Settings>().EnableFeatureByDefault<T>();
        return settings;
    }

    /// <summary>
    /// TODO Obsolete?
    /// Marks the given feature as enabled by default.
    /// </summary>
    public static SettingsHolder EnableFeatureByDefault(this SettingsHolder settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(featureType);

        settings.Get<FeatureComponent.Settings>().EnableFeatureByDefault(featureType);
        return settings;
    }

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
    /// TODO Obsolete?
    /// Returns if a given feature has been enabled in this endpoint.
    /// </summary>
    public static bool IsFeatureEnabled(this IReadOnlySettings settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(featureType);
        return settings.Get<FeatureComponent.Settings>().IsFeature(featureType, FeatureState.Enabled);
    }

    internal static bool IsFeatureEnabled<T>(this IReadOnlySettings settings) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Get<FeatureComponent.Settings>().IsFeature<T>(FeatureState.Enabled);
    }

    internal static void EnableFeature<T>(this SettingsHolder settings) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Get<FeatureComponent.Settings>().EnableFeature<T>();
    }

    internal static void EnableFeature(this SettingsHolder settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Get<FeatureComponent.Settings>().EnableFeature(featureType);
    }

    internal static void DisableFeature<T>(this SettingsHolder settings) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Get<FeatureComponent.Settings>().DisableFeature<T>();
    }

    internal static void DisableFeature(this SettingsHolder settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Get<FeatureComponent.Settings>().DisableFeature(featureType);
    }
}