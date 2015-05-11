namespace NServiceBus.Transports
{
    /// <summary>
    /// Called when the bus wants to defer a message
    /// </summary>
    public interface IDeferMessages
    {
        /// <summary>
        /// Defers the given message
        /// </summary>
        void Defer(OutgoingMessage message, TransportDeferOptions options);

        /// <summary>
        /// Clears all timeouts for the given header
        /// </summary>
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}