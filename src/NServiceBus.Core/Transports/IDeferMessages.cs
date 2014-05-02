namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    /// Called when the bus wants to defer a message
    /// </summary>
    public interface IDeferMessages
    {
        /// <summary>
        /// Defers the given message that will be processed at the given <see cref="DateTime"/>
        /// </summary>
        /// <param name="address">The endpoint of the endpoint who should get the message</param>
        void Defer(TransportMessage message, DateTime processAt, Address address);

        /// <summary>
        /// Defers the given message that will be processed at the given <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="address">The endpoint of the endpoint who should get the message</param>
        void Defer(TransportMessage message, TimeSpan delayBy, Address address);

        /// <summary>
        /// Clears all timeouts for the given header
        /// </summary>
        void ClearDeferredMessages(string headerKey, string headerValue);
    }
}