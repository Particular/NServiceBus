namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Data containing multiple messages for raising in events.
    /// </summary>
    public class MessagesEventArgs : EventArgs
    {
        /// <summary>
        /// Instantiate an event arg referencing multiple messages.
        /// </summary>
        public MessagesEventArgs(object[] messages)
        {
            Messages = messages;
        }

        /// <summary>
        /// The messages that were sent.
        /// </summary>
        public object[] Messages { get; private set; }
    }
}