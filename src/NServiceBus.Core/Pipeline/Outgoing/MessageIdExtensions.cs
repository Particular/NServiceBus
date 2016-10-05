namespace NServiceBus
{
    using Extensibility;

    /// <summary>
    /// Extensions to the outgoing pipeline.
    /// </summary>
    public static class MessageIdExtensions
    {
        /// <summary>
        /// Allows the user to set the message id.
        /// </summary>
        /// <param name="options">Options to extend.</param>
        /// <param name="messageId">The message id to use.</param>
        public static void SetMessageId(this ExtendableOptions options, string messageId)
        {
            Guard.AgainstNullAndEmpty(messageId, messageId);

            options.MessageId = messageId;
        }

        /// <summary>
        /// Returns the message id.
        /// </summary>
        /// <param name="options">Options to extend.</param>
        /// <returns>The message id.</returns>
        public static string GetMessageId(this ExtendableOptions options)
        {
            return options.MessageId;
        }
    }
}