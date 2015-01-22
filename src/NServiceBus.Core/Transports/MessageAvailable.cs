namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Informs the NServiceBus executor that a message is available for processing.
    /// </summary>
    public class MessageAvailable
    {
        readonly Action<IncomingContext> contextAction;
        readonly string publicReceiveAddress;

        /// <summary>
        /// Creates new instance of <see cref="MessageAvailable"/>
        /// </summary>
        /// <param name="publicReceiveAddress">Public receive address for this message</param>
        /// <param name="contextAction">A callback that sets up the pipeline context for processing a received message.</param>
        public MessageAvailable(string publicReceiveAddress, Action<IncomingContext> contextAction)
        {
            this.contextAction = contextAction;
            this.publicReceiveAddress = publicReceiveAddress;
        }

        /// <summary>
        /// Gets the public receive address for this message.
        /// </summary>
        public string PublicReceiveAddress
        {
            get { return publicReceiveAddress; }
        }

        internal void InitializeContext(IncomingContext context)
        {
            contextAction(context);
        }
    }
}