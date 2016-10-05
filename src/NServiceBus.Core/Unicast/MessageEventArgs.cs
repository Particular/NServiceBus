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
        public MessageEventArgs(object msg)
        {
            Message = msg;
        }

        /// <summary>
        /// The message.
        /// </summary>
        public object Message { get; private set; }
    }
}