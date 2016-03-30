namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extension methods for easier type safe access to instances of <see cref="OutgoingMessage{TMessage,TOptions}" />
    /// </summary>
    public static class OutgoingMessageExtensions
    {
        /// <summary>
        /// Returns all <see cref="RepliedMessage{TMessage}" /> of the specified type contained in
        /// <paramref name="repliedMessages" />.
        /// </summary>
        public static IEnumerable<RepliedMessage<TMessage>> Containing<TMessage>(this IEnumerable<RepliedMessage<object>> repliedMessages)
        {
            return repliedMessages
                .Where(x => x.Message is TMessage)
                .Select(x => new RepliedMessage<TMessage>((TMessage)x.Message, x.Options));
        }

        /// <summary>
        /// Returns all <see cref="PublishedMessage{TMessage}" /> of the specified type contained in
        /// <paramref name="publishedMessages" />.
        /// </summary>
        public static IEnumerable<PublishedMessage<TMessage>> Containing<TMessage>(this IEnumerable<PublishedMessage<object>> publishedMessages)
        {
            return publishedMessages
                .Where(x => x.Message is TMessage)
                .Select(x => new PublishedMessage<TMessage>((TMessage)x.Message, x.Options));
        }

        /// <summary>
        /// Returns all <see cref="SentMessage{TMessage}" /> of the specified type contained in <paramref name="sentMessages" />.
        /// </summary>
        public static IEnumerable<SentMessage<TMessage>> Containing<TMessage>(this IEnumerable<SentMessage<object>> sentMessages)
        {
            return sentMessages
                .Where(x => x.Message is TMessage)
                .Select(x => new SentMessage<TMessage>((TMessage)x.Message, x.Options));
        }

        internal static IEnumerable<TimeoutMessage<TMessage>> Containing<TMessage>(this IEnumerable<TimeoutMessage<object>> timeoutMessages)
        {
            return timeoutMessages
                .Where(x => x.Message is TMessage)
                .Select(x => new TimeoutMessage<TMessage>((TMessage)x.Message, x.Options, x.Within));
        }

        /// <summary>
        /// Tries to cast the message contained in <paramref name="sentMessage" /> to <typeparamref name="TMessage" />.
        /// </summary>
        public static TMessage Message<TMessage>(this RepliedMessage<object> sentMessage) where TMessage : class
        {
            return sentMessage.Message as TMessage;
        }

        /// <summary>
        /// Tries to cast the message contained in <paramref name="sentMessage" /> to <typeparamref name="TMessage" />.
        /// </summary>
        public static TMessage Message<TMessage>(this PublishedMessage<object> sentMessage) where TMessage : class
        {
            return sentMessage.Message as TMessage;
        }

        /// <summary>
        /// Tries to cast the message contained in <paramref name="sentMessage" /> to <typeparamref name="TMessage" />.
        /// </summary>
        public static TMessage Message<TMessage>(this SentMessage<object> sentMessage) where TMessage : class
        {
            return sentMessage.Message as TMessage;
        }
    }
}