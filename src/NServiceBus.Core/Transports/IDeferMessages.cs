namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    /// Called when the bus wants to defer a message
    /// </summary>
    public interface IDeferMessages
    {
        /// <summary>
        /// Defers the given message that will be processed at the given time
        /// </summary>
        /// <param name="message"></param>
        /// <param name="processAt"></param>
        /// <param name="address">The endpoint of the endpoint who should get the message</param>
        void Defer(TransportMessage message, DateTime processAt, Address address);

        /// <summary>
        /// Clears all timeouts for the given header
        /// </summary>
        /// <param name="headerKey"></param>
        /// <param name="headerValue"></param>
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}