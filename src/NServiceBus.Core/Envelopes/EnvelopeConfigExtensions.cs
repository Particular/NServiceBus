#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using Features;

/// <summary>
/// Provides extensions to register envelope handlers.
/// </summary>
public static class EnvelopeConfigExtensions
{
    extension(FeatureConfigurationContext context)
    {
        /// <summary>
        /// Adds the envelope handler type.
        /// </summary>
        public void AddEnvelopeHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>() where THandler : class, IEnvelopeHandler
        {
            ArgumentNullException.ThrowIfNull(context);

            context.Settings.Get<EnvelopeComponent.Settings>().AddEnvelopeHandler<THandler>();
        }
    }
}