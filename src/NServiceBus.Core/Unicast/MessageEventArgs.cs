namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Data containing a message for raising in events.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Instantiates a new object containing the given message.
        /// </summary>
        /// <param name="msg"></param>
        public MessageEventArgs(object msg)
        {
            Message = msg;
        }

        /// <summary>
        /// The message.
        /// </summary>
        public object Message { get; private set; }
    }

    /// <summary>
    /// Data containing multiple messages for raising in events.
    /// </summary>
    public class MessagesEventArgs : EventArgs
    {
        /// <summary>
        /// Instantiate an event arg referencing multiple messages.
        /// </summary>
        /// <param name="messages"></param>
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
