namespace NServiceBus
{
    using System;

    /// <summary>
    /// Syntactic sugar for ISendOnlyBus
    /// </summary>
    public static class ISendOnlyBusExtensions
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="message">The message to send.</param>
        public static ICallback Send(this ISendOnlyBus bus, object message)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(message, "message");

            return bus.Send(message, new SendOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <remarks>
        /// The message will be sent to the destination configured for T
        /// </remarks>
        public static ICallback Send<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            return bus.Send(messageConstructor, new SendOptions());
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="destination">
        /// The address of the destination to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        public static ICallback Send(this ISendOnlyBus bus, string destination, object message)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(message, "message");

            return bus.Send(message, new SendOptions(destination));
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="bus"></param>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static ICallback Send<T>(this ISendOnlyBus bus, string destination, Action<T> messageConstructor)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            return bus.Send(messageConstructor, new SendOptions(destination));

        }
    }
}