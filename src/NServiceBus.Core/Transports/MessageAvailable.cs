namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Informs the NServiceBus executor that a message is available for processing.
    /// </summary>
    public class MessageAvailable
    {
        Action<IncomingContext> contextAction;

        /// <summary>
        /// Initializes new instance of <see cref="MessageAvailable"/>.
        /// </summary>
        /// <param name="contextAction">A callback that sets up the pipeline context for processing a received message.</param>
        public MessageAvailable(Action<IncomingContext> contextAction)
        {
            Guard.AgainstNull("contextAction", contextAction);
            this.contextAction = contextAction;
        }

        internal void InitializeContext(IncomingContext context)
        {
            contextAction(context);
        }
    }
}