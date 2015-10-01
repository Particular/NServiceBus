namespace NServiceBus.Pipeline
{
    using Contexts;
    using NServiceBus.Transports;

    /// <summary>
    /// Context extension to provide access to the incoming physical message.
    /// </summary>
    public static class IncomingMessageContextExtensions
    {
        /// <summary>
        /// Returns the incoming physical message if there is one currently processed.
        /// </summary>
        public static bool TryGetIncomingPhysicalMessage(this OutgoingContext context, out IncomingMessage message)
        {
            Guard.AgainstNull("context", context);

            return context.TryGet(out message);
        }
    }
}