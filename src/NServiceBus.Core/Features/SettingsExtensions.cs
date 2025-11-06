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
    /// <remarks>Enabling features is intended to be used for downstream components that only have access to settings. Features that need to enable other features should use <see cref="Feature.Enable{TFeature}"/>.</remarks>
    /// <typeparam name="TFeature">The feature to enable.</typeparam>
    public static void EnableFeature<TFeature>(this SettingsHolder settings) where TFeature : Feature, new()
        => settings.Get<FeatureComponent.Settings>().EnableFeature<TFeature>();

    /// <summary>
    /// Returns if a given feature has been activated in this endpoint.
    /// </summary>
    public static bool IsFeatureActive<TFeature>(this IReadOnlySettings settings) where TFeature : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Get<FeatureComponent.Settings>().IsFeature<TFeature>(FeatureState.Active);
    }

    /// <summary>
    /// Returns if a given feature has been enabled in this endpoint.
    /// </summary>
    public static bool IsFeatureEnabled<TFeature>(this IReadOnlySettings settings) where TFeature : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.Get<FeatureComponent.Settings>().IsFeature<TFeature>(FeatureState.Enabled);
    }

    internal static void DisableFeature<TFeature>(this IReadOnlySettings settings) where TFeature : Feature, new()
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Get<FeatureComponent.Settings>().DisableFeature<TFeature>();
    }
}