namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Syntactic sugar for <see cref="IMessageProcessingContext" />.
    /// </summary>
    public static class IMessageProcessingContextExtensions
    {
        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="context">Object being extended.</param>
        /// <param name="message">The message to send.</param>
        public static Task Reply(this IMessageProcessingContext context, object message)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(message), message);

            return context.Reply(message, new ReplyOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and performs a regular Reply.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="context">Object being extended.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task Reply<T>(this IMessageProcessingContext context, Action<T> messageConstructor)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);

            return context.Reply(messageConstructor, new ReplyOptions());
        }
    }
}