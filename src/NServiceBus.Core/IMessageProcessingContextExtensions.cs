namespace NServiceBus
{
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
    }
}