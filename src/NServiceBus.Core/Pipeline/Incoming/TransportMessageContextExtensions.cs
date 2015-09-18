namespace NServiceBus.Pipeline
{
    using NServiceBus.Pipeline.Contexts;
    using OutgoingPipeline;

    /// <summary>
    /// Context extension to provide access to the incoming physical message.
    /// </summary>
    public static class TransportMessageContextExtensions
    {
        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this OutgoingReplyContext context, out TransportMessage message)
        {
            Guard.AgainstNull("context", context);

            return context.TryGet(out message);
        }

        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this OutgoingLogicalMessageContext context, out TransportMessage message)
        {
            Guard.AgainstNull("context", context);

            return context.TryGet(out message);
        }

        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this OutgoingPhysicalMessageContext context, out TransportMessage message)
        {
            Guard.AgainstNull("context", context);

            return context.TryGet(out message);
        }
    }
}