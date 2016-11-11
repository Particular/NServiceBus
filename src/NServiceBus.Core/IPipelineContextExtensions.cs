namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// Syntactic sugar for <see cref="IPipelineContext" />.
    /// </summary>
    public static class IPipelineContextExtensions
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
        /// <param name="message">The message to send.</param>
        public static Task Send(this IPipelineContext context, object message)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(message), message);

            return context.Send(message, new SendOptions());
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
        /// <param name="destination">The address of the destination to which the message will be sent.</param>
        /// <param name="message">The message to send.</param>
        public static Task Send(this IPipelineContext context, string destination, object message)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNullAndEmpty(nameof(destination), destination);
            Guard.AgainstNull(nameof(message), message);

            var options = new SendOptions();

            options.SetDestination(destination);

            return context.Send(message, options);
        }

        /// <summary>
        /// Sends the message back to the current endpoint.
        /// </summary>
        /// <param name="context">Object being extended.</param>
        /// <param name="message">The message to send.</param>
        public static Task SendLocal(this IPipelineContext context, object message)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(message), message);

            var options = new SendOptions();

            options.RouteToThisEndpoint();

            return context.Send(message, options);
        }

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="context">The instance of <see cref="IPipelineContext" /> to use for the action.</param>
        /// <param name="message">The message to publish.</param>
        public static Task Publish(this IPipelineContext context, object message)
        {
            Guard.AgainstNull(nameof(context), context);
            Guard.AgainstNull(nameof(message), message);

            return context.Publish(message, new PublishOptions());
        }
    }
}