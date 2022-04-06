namespace NServiceBus
{
    /// <summary>
    /// Gives users control of message conversations.
    /// </summary>
    public static class ConversationRoutingExtensions
    {
        /// <summary>
        /// Start a new messaging conversation.
        /// </summary>
        /// <param name="sendOptions">The option being extended.</param>
        /// <param name="conversationId">The id for the new conversation. If not provided, an id will be generated.</param>
        public static void StartNewConversation(this SendOptions sendOptions, string conversationId = null)
        {
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            sendOptions.Context.Set(AttachCausationHeadersBehavior.NewConversationId, conversationId);
        }
    }
}