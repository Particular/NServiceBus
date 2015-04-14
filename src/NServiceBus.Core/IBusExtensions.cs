namespace NServiceBus
{
    using System;

    /// <summary>
    /// Syntactic sugar for IBus
    /// </summary>
    public static class IBusExtensions
    {
        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="message">The message to send.</param>
        public static ICallback SendLocal(this IBus bus, object message)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(message, "message");

            return bus.SendLocal(message, new SendLocalOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static ICallback SendLocal<T>(this IBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            return bus.SendLocal(messageConstructor, new SendLocalOptions());
        }
    }
}