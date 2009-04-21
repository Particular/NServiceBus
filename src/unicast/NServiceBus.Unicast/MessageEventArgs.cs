using NServiceBus.Messages;
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
            this.Message = msg;
        }

        /// <summary>
        /// The message.
        /// </summary>
        public IMessage Message { get; private set; }
    }
}
