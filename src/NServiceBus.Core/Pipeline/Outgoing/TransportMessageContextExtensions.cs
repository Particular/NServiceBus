namespace NServiceBus.Pipeline.Outgoing
{
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Context extension to provide access to the incoming physical message.
    /// </summary>
    public static class TransportMessageContextExtensions
    {
        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this OutgoingReplyContext context, out IncomingMessage message)
        {
            Guard.AgainstNull(nameof(context), context);

            return context.TryGet(out message);
        }

        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this OutgoingLogicalMessageContext context, out IncomingMessage message)
        {
            Guard.AgainstNull(nameof(context), context);

            return context.TryGet(out message);
        }

        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this OutgoingPhysicalMessageContext context, out IncomingMessage message)
        {
            Guard.AgainstNull(nameof(context), context);

            return context.TryGet(out message);
        }
    }
}