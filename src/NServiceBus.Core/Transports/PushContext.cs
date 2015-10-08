namespace NServiceBus.Transports
{
    using Extensibility;

    /// <summary>
    ///     Allows the transport to pass relevant info to the pipeline.
    /// </summary>
    public class PushContext
    {
        /// <summary>
        ///     Initializes the context.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="context">Any context that the transport wants to be available on the pipeline.</param>
        public PushContext(IncomingMessage message, ContextBag context)
        {
            Guard.AgainstNull("message", message);
            Guard.AgainstNull("context", context);

            Message = message;
            Context = context;
        }

        /// <summary>
        ///     Context provided by the transport.
        /// </summary>
        public ContextBag Context { get; private set; }

        /// <summary>
        ///     The incoming message to be processed.
        /// </summary>
        public IncomingMessage Message { get; private set; }
    }
}