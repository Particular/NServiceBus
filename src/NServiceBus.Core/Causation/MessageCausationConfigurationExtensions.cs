namespace NServiceBus
{
    /// <summary>
    /// Provides configuration option for message causation.
    /// </summary>
    public static class MessageCausationConfigurationExtensions
    {
        /// <summary>
        /// Allows customization of conversation ID's for individual messages. Use this to provide domain specific conversation ID's.
        /// </summary>
        /// <param name="endpointConfiguration">The configuration object being extended.</param>
        /// <param name="tryGetConversationIdDelegate">The delegate that will try to determine the conversation id. If not possible a generated COMB guid will be used.</param>
        public static void CustomConversationIdStrategy(this EndpointConfiguration endpointConfiguration, TryGetConversationIdDelegate tryGetConversationIdDelegate)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(tryGetConversationIdDelegate), tryGetConversationIdDelegate);

            endpointConfiguration.Settings.Set<TryGetConversationIdDelegate>(tryGetConversationIdDelegate);
        }
    }
}