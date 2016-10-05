namespace NServiceBus.Pipeline
{
    using Transport;

    /// <summary>
    /// Context extension to provide access to the incoming physical message.
    /// </summary>
    public static class TransportMessageContextExtensions
    {
        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this IOutgoingReplyContext context, out IncomingMessage message)
        {
            Guard.AgainstNull(nameof(context), context);

            return context.Extensions.TryGet(out message);
        }

        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this IOutgoingLogicalMessageContext context, out IncomingMessage message)
        {
            Guard.AgainstNull(nameof(context), context);

            return context.Extensions.TryGet(out message);
        }

        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this IOutgoingPhysicalMessageContext context, out IncomingMessage message)
        {
            Guard.AgainstNull(nameof(context), context);

            return context.Extensions.TryGet(out message);
        }
    }
}