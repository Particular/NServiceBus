namespace NServiceBus.Unicast.Transport
{
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Transports;

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
            var message = new OutgoingMessage(CombGuid.Generate().ToString(),new Dictionary<string, string>(), Stream.Null);
            message.Headers[Headers.ControlMessageHeader] = true.ToString();
            message.Headers[Headers.MessageIntent] = intent.ToString();

            return message;
        }
    }
}
