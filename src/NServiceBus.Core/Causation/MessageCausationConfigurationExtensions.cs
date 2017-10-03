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
        /// <param name="customConversationIdDelegate">The delegate that will determine the conversation id.</param>
        public static void CustomConversationId(this EndpointConfiguration endpointConfiguration, CustomConversationIdDelegate customConversationIdDelegate)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(customConversationIdDelegate), customConversationIdDelegate);

            endpointConfiguration.Settings.Set<CustomConversationIdDelegate>(customConversationIdDelegate);
        }
    }

    /// <summary>
    /// Allows customization of conversation ID's for individual messages. Return false to use the default COMB-Guid strategy.
    /// </summary>
    /// <param name="context">Context for the conversation id generation.</param>
    /// <param name="customConverationId">The custom conversation id to be used for this message.</param>
    /// <returns>`true` if the returned conversation id should be used, `false` otherwise.</returns>
    public delegate bool CustomConversationIdDelegate(CustomConversationIdContext context, out string customConverationId);
}