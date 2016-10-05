namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// Helper for creating control messages.
    /// </summary>
    public static class ControlMessageFactory
    {
        /// <summary>
        /// Creates Transport Message.
        /// </summary>
        /// <returns>Transport Message.</returns>
        public static OutgoingMessage Create(MessageIntentEnum intent)
        {
            var message = new OutgoingMessage(CombGuid.Generate().ToString(), new Dictionary<string, string>(), new byte[0]);
            message.Headers[Headers.ControlMessageHeader] = Boolean.TrueString;
            message.Headers[Headers.MessageIntent] = intent.ToString();

            return message;
        }
    }
}