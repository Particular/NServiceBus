﻿#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Provides configuration options for message causation.
/// </summary>
public static class MessageCausationConfigurationExtensions
{
    /// <summary>
    /// Customizes conversation IDs for individual messages. Use this to provide domain-specific conversation
    /// IDs.
    /// </summary>
    public static void CustomConversationIdStrategy(this EndpointConfiguration endpointConfiguration, Func<ConversationIdStrategyContext, ConversationId> customStrategy)
    {
        ArgumentNullException.ThrowIfNull(endpointConfiguration);
        ArgumentNullException.ThrowIfNull(customStrategy);

        endpointConfiguration.Settings.Set("CustomConversationIdStrategy", customStrategy);
    }
}