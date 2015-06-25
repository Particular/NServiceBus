namespace NServiceBus.OutgoingPipeline.Send
{
    using NServiceBus.Pipeline;

    /// <summary>
    /// Pipeline context for send operations
    /// </summary>
    public class OutgoingSendContext : BehaviorContext
    {
        /// <summary>
        /// Initializes the context with a parent context
        /// </summary>
        public OutgoingSendContext(BehaviorContext parentContext, OutgoingLogicalMessage message, SendOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull(parentContext, "parentContext");
            Guard.AgainstNull(message, "message");
            Guard.AgainstNull(options, "options");

            Set(message);

            Merge(options.Context);
        }
    }
}