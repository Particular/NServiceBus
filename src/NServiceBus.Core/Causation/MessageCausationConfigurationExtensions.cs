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
        public static void CustomConversationId(this EndpointConfiguration endpointConfiguration, TryGetConversationIdDelegate tryGetConversationIdDelegate)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(tryGetConversationIdDelegate), tryGetConversationIdDelegate);

            endpointConfiguration.Settings.Set<TryGetConversationIdDelegate>(tryGetConversationIdDelegate);
        }
    }

    /// <summary>
    /// Allows customization of conversation ID's for individual messages. Return false to use the default COMB-Guid strategy.
    /// </summary>
    /// <param name="context">Context for the conversation id generation.</param>
    /// <param name="customConverationId">The custom conversation id to be used for this message.</param>
    /// <returns>`true` if the returned conversation id should be used, `false` otherwise.</returns>
    public delegate bool TryGetConversationIdDelegate(CustomConversationIdContext context, out string customConverationId);
}