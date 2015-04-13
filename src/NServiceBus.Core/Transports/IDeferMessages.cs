namespace NServiceBus.Transports
{
    using Unicast;

    /// <summary>
    /// Called when the bus wants to defer a message
    /// </summary>
    public interface IDeferMessages
    {
        /// <summary>
        /// Defers the given message
        /// </summary>
        void Defer(OutgoingMessage message, SendMessageOptions sendMessageOptions);

        /// <summary>
        /// Clears all timeouts for the given header
        /// </summary>
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}