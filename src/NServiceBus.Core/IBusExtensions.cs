namespace NServiceBus
{
    using System;

    /// <summary>
    /// Syntactic sugar for IBus
    /// </summary>
    public static class IBusExtensions
    {
        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="message">The message to send.</param>
        public static void Reply(this IBus bus, object message)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(message, "message");

            bus.Reply(message,new ReplyOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and performs a regular Reply.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static void Reply<T>(this IBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            bus.Reply(messageConstructor, new ReplyOptions());
        }
        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="message">The message to send.</param>
        public static void SendLocal(this IBus bus, object message)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(message, "message");

            bus.SendLocal(message, new SendLocalOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static void SendLocal<T>(this IBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            bus.SendLocal(messageConstructor, new SendLocalOptions());
        }
    }
}