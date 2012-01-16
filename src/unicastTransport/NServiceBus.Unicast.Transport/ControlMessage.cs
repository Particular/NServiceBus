namespace NServiceBus.Unicast.Transport
{
    using System.Collections.Generic;

    /// <summary>
    /// Helper for creating controll messages
    /// </summary>
    public static class ControlMessage
    {
        /// <summary>
        /// Creates Transport Message
        /// </summary>
        /// <returns>Transport Message</returns>
        public static TransportMessage Create()
        {
            var transportMessage = new TransportMessage
                                       {
                                           ReplyToAddress = Address.Local,
                                           Headers = new Dictionary<string, string>(),
                                           Recoverable = true,
                                           MessageIntent = MessageIntentEnum.Send
                                       };
            transportMessage.Headers.Add(ControlMessageHeader, true.ToString());

            return transportMessage;
        }

        /// <summary>
        /// Header which tells that this transportmessage is a controll message
        /// </summary>
        public static string ControlMessageHeader = "NServiceBus.ControlMessage";
    }

    /// <summary>
    /// Extensions to make the usage if control messages easier
    /// </summary>
    public static class TransportMessageExtensions
    {
        /// <summary>
        /// True if the transportmessage is a control message
        /// </summary>
        /// <param name="transportMessage"></param>
        /// <returns></returns>
        public static bool IsControlMessage(this TransportMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(ControlMessage.ControlMessageHeader);
        }

    }
}