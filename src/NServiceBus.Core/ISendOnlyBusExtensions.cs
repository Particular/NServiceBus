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
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(message, "message");

            var options = new SendOptions(destination);

            return bus.Send(message, options);
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
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            var context = new SendOptions(destination);

            return bus.Send(messageConstructor, context);

        }

        /// <summary>
        /// Sends the message to the destination as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        public static ICallback Send(this ISendOnlyBus bus, string destination, string correlationId, object message)
        {
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNullAndEmpty(correlationId, "correlationId");
            Guard.AgainstNull(message, "message");

            var context = new SendOptions(destination, correlationId);

            return bus.Send(message, context);
        }

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the destination identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        public static ICallback Send<T>(this ISendOnlyBus bus, string destination, string correlationId, Action<T> messageConstructor)
        {
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNullAndEmpty(correlationId, "correlationId");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            var context = new SendOptions(destination, correlationId);

            return bus.Send(messageConstructor, context);
        }

    }
}