namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Helper methods for <see cref="IIncomingPhysicalMessageContext"/>.
    /// </summary>
    public static class IIncomingPhysicalMessageContextExtensions
    {
        /// <summary>
        /// Gets the message intent from the headers.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The message intent.</returns>
        public static MessageIntentEnum GetMesssageIntent(this IIncomingPhysicalMessageContext context)
        {
            var messageIntent = default(MessageIntentEnum);

            string messageIntentString;
            if (context.Headers.TryGetValue(Headers.MessageIntent, out messageIntentString))
            {
                Enum.TryParse(messageIntentString, true, out messageIntent);
            }

            return messageIntent;
        }

        /// <summary>
        /// Gets the reply to address.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The reply to address.</returns>
        public static string GetReplyToAddress(this IIncomingPhysicalMessageContext context)
        {
            string replyToAddress;

            return context.Headers.TryGetValue(Headers.ReplyToAddress, out replyToAddress) ? replyToAddress : null;
        }
    }
}