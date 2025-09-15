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
    /// Marks the given feature as enabled by default.
    /// </summary>
    public static SettingsHolder EnableFeatureByDefault<T>(this SettingsHolder settings) where T : Feature
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnableFeatureByDefault(typeof(T));
        return settings;
    }

    /// <summary>
    /// Marks the given feature as enabled by default.
    /// </summary>
    public static SettingsHolder EnableFeatureByDefault(this SettingsHolder settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(featureType);

        // Default values etc needs double checking. This is a hack
        if (settings.Get<FeatureComponent.Settings>().Features
            .TryGetValue(featureType, out var state))
        {
            state.Default = FeatureState.Enabled;
        }
        else
        {
            settings.Get<FeatureComponent.Settings>().Features[featureType] = (FeatureState.Enabled, null);
        }
        // TODO backward compatibility?
        //settings.SetDefault(featureType.FullName, FeatureState.Enabled);
        return settings;
    }

    /// <summary>
    /// Returns if a given feature has been activated in this endpoint.
    /// </summary>
    public static bool IsFeatureActive(this IReadOnlySettings settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(featureType);

        if (settings.Get<FeatureComponent.Settings>().Features.TryGetValue(featureType, out var featureState))
        {
            return featureState.Override.GetValueOrDefault(featureState.Default.GetValueOrDefault()) == FeatureState.Active;
        }
        return false;
    }

    /// <summary>
    /// Returns if a given feature has been enabled in this endpoint.
    /// </summary>
    public static bool IsFeatureEnabled(this IReadOnlySettings settings, Type featureType)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(featureType);

        if (settings.Get<FeatureComponent.Settings>().Features.TryGetValue(featureType, out var featureState))
        {
            return featureState.Override.GetValueOrDefault(featureState.Default.GetValueOrDefault()) == FeatureState.Enabled;
        }
        return false;
    }

    internal static void EnableFeature(this SettingsHolder settings, Type featureType)
    {
        if (settings.Get<FeatureComponent.Settings>().Features
            .TryGetValue(featureType, out var state))
        {
            state.Override = FeatureState.Enabled;
        }
        else
        {
            settings.Get<FeatureComponent.Settings>().Features[featureType] = (null, FeatureState.Enabled);
        }
        // TODO backward compatibility?
        // settings.Set(featureType.FullName, FeatureState.Enabled);
    }

    internal static void DisableFeature(this SettingsHolder settings, Type featureType)
    {
        if (settings.Get<FeatureComponent.Settings>().Features
            .TryGetValue(featureType, out var state))
        {
            state.Override = FeatureState.Disabled;
        }
        else
        {
            settings.Get<FeatureComponent.Settings>().Features[featureType] = (null, FeatureState.Disabled);
        }
        // TODO backward compatibility?
        //settings.Set(featureType.FullName, FeatureState.Disabled);
    }

    internal static void MarkFeatureAsActive(this SettingsHolder settings, Type featureType)
    {
        if (settings.Get<FeatureComponent.Settings>().Features
            .TryGetValue(featureType, out var state))
        {
            state.Override = FeatureState.Active;
        }
        else
        {
            settings.Get<FeatureComponent.Settings>().Features[featureType] = (null, FeatureState.Active);
        }
        // TODO backward compatibility?
        //settings.Set(featureType.FullName, FeatureState.Active);
    }

    internal static void MarkFeatureAsDeactivated(this SettingsHolder settings, Type featureType)
    {
        if (settings.Get<FeatureComponent.Settings>().Features
            .TryGetValue(featureType, out var state))
        {
            state.Override = FeatureState.Deactivated;
        }
        else
        {
            settings.Get<FeatureComponent.Settings>().Features[featureType] = (null, FeatureState.Deactivated);
        }
        // TODO backward compatibility?
        //settings.Set(featureType.FullName, FeatureState.Deactivated);
    }
}