namespace NServiceBus
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Extensions to the outgoing pipeline.
    /// </summary>
    public static class MessageIdExtensions
    {
        /// <summary>
        /// Allows the user to set the message id.
        /// </summary>
        /// <param name="context">Context to extend.</param>
        /// <param name="messageId">The message id to use.</param>
        public static void SetMessageId(this ExtendableOptions context, string messageId)
        {
            Guard.AgainstNullAndEmpty(messageId, messageId);

            context.MessageId = messageId;
        }
    }
}