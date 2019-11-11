namespace NServiceBus
{
    using System;
    using Pipeline.Outgoing;

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
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(customStrategy), customStrategy);

            endpointConfiguration.Settings.Get<SendComponent.Configuration>().CustomConversationIdStrategy = customStrategy;
        }
    }
}