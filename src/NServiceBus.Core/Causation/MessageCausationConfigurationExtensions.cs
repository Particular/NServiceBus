namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides configuration option for message causation.
    /// </summary>
    public static class MessageCausationConfigurationExtensions
    {
        /// <summary>
        /// Allows customization of conversation ID's for individual messages. Use this to provide domain specific conversation
        /// ID's.
        /// </summary>
        public static void CustomConversationIdStrategy(this EndpointConfiguration endpointConfiguration, Func<ConversationIdStrategyContext, ConversationId> customStrategy)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(customStrategy), customStrategy);

            endpointConfiguration.Settings.Set("CustomConversationIdStrategy", customStrategy);
        }
    }
}