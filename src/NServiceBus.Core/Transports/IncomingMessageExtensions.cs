namespace NServiceBus.Transport
{
    using System;

    /// <summary>
    /// Helper methods for <see cref="IncomingMessage" />.
    /// </summary>
    public static class IncomingMessageExtensions
    {
        /// <summary>
        /// Gets the message intent from the headers.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The message intent.</returns>
        public static MessageIntent GetMessageIntent(this IncomingMessage message)
        {
            Guard.ThrowIfNull(message);
            var messageIntent = default(MessageIntent);

            if (message.Headers.TryGetValue(Headers.MessageIntent, out var messageIntentString))
            {
                Enum.TryParse(messageIntentString, true, out messageIntent);
            }

            return messageIntent;
        }

        /// <summary>
        /// Gets the reply to address.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The reply to address.</returns>
        public static string GetReplyToAddress(this IncomingMessage message)
        {
            Guard.ThrowIfNull(message);
            return message.Headers.TryGetValue(Headers.ReplyToAddress, out var replyToAddress) ? replyToAddress : null;
        }
    }
}