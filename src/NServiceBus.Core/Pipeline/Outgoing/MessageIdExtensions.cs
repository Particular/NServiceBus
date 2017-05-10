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

            options.UserDefinedMessageId = messageId;
        }

        /// <summary>
        /// Returns the configured message id.
        /// </summary>
        /// <param name="options">Options to extend.</param>
        /// <returns>The message id if configured or <c>null</c>.</returns>
        public static string GetMessageId(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);
            return options.UserDefinedMessageId;
        }
    }
}