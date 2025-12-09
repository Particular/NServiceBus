#nullable enable

namespace NServiceBus;

using System;
using Features;

/// <summary>
/// Provides extensions to register envelope handlers.
/// </summary>
public static class EnvelopeConfigExtensions
{
    /// <summary>
    /// Adds the envelope handler type.
    /// </summary>
    public static void AddEnvelopeHandler<THandler>(this FeatureConfigurationContext context) where THandler : class, IEnvelopeHandler
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Settings.Get<EnvelopeComponent.Settings>().AddEnvelopeHandler<THandler>();
    }
}