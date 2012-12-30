namespace NServiceBus.Unicast.Queuing
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
        void Defer(TransportMessage message, DateTime processAt);

        /// <summary>
        /// Clears all timeouts for the given header
        /// </summary>
        /// <param name="headerKey"></param>
        /// <param name="headerValue"></param>
        void ClearDeferedMessages(string headerKey, string headerValue);

    }
}