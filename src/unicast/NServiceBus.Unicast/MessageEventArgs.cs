using System;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Data containing a message for raising in events.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// Instantiates a new object containing the given message.
        /// </summary>
        /// <param name="msg"></param>
        public MessageEventArgs(IMessage msg)
        {
            Message = msg;
        }

        /// <summary>
        /// The message.
        /// </summary>
        public IMessage Message { get; private set; }
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
        public MessagesEventArgs(IMessage[] messages)
        {
            Messages = messages;
        }

        /// <summary>
        /// The messages that were sent.
        /// </summary>
        public IMessage[] Messages { get; private set; }
    }
}
