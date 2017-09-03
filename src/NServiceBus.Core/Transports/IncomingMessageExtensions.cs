namespace NServiceBus.Transport
{
    using System;

    /// <summary>
    /// Helper methods for <see cref="IncomingMessage" />.
    /// </summary>
    public static partial class IncomingMessageExtensions
    {
        /// <summary>
        /// Gets the message intent from the headers.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The message intent.</returns>
        public static MessageIntentEnum GetMessageIntent(this IncomingMessage message)
        {
            Guard.AgainstNull(nameof(message), message);
            var messageIntent = default(MessageIntentEnum);

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
            Guard.AgainstNull(nameof(message), message);
            return message.Headers.TryGetValue(Headers.ReplyToAddress, out var replyToAddress) ? replyToAddress : null;
        }
    }
}