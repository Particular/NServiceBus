namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides configuration option for message causation.
    /// </summary>
    public static class MessageCausationConfigurationExtensions
    {
        /// <summary>
        /// Overrides the default conversation id generator where messages are assigned a COMB-Guid as conversation id. Use this to provide domain specific conversation ID's.
        /// </summary>
        /// <param name="endpointConfiguration">The configuration object being extended.</param>
        /// <param name="customGenerator">The custom conversation id convention.</param>
        public static void CustomConversationIdGenerator(this EndpointConfiguration endpointConfiguration, Func<ConversationIdGeneratorContext, string> customGenerator)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);
            Guard.AgainstNull(nameof(customGenerator), customGenerator);

            endpointConfiguration.Settings.Set(CustomConversationIdGeneratorKey, customGenerator);
        }

        internal static string CustomConversationIdGeneratorKey = "MessageCausation.CustomConversationIdGenerator";
    }
}