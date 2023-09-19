namespace NServiceBus
{
    using System;
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
            ArgumentNullException.ThrowIfNullOrEmpty(messageId);

            options.UserDefinedMessageId = messageId;
        }

        /// <summary>
        /// Returns the configured message id.
        /// </summary>
        /// <param name="options">Options to extend.</param>
        /// <returns>The message id if configured or <c>null</c>.</returns>
        public static string GetMessageId(this ExtendableOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return options.UserDefinedMessageId;
        }
    }
}