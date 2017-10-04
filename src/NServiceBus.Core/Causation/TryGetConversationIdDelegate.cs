namespace NServiceBus
{
    /// <summary>
    /// Allows customization of conversation ID's for individual messages. Return false to use the default COMB-Guid strategy.
    /// </summary>
    /// <param name="context">Context for the conversation id generation.</param>
    /// <param name="customConverationId">The custom conversation id to be used for this message.</param>
    /// <returns>`true` if the returned conversation id should be used, `false` otherwise.</returns>
    public delegate bool TryGetConversationIdDelegate(ConversationIdStrategyContext context, out string customConverationId);
}