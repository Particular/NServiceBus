namespace NServiceBus
{
    using System;

    /// <summary>
    /// Syntactic sugar for ISendOnlyBus
    /// </summary>
    public static class ISendOnlyBusExtensions
    {
       
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="message">The message to publish</param>
        public static void Publish(this ISendOnlyBus bus, object message)
        {
            bus.Publish(message, new PublishOptions());
        }

       
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <typeparam name="T">The message type</typeparam>
        public static void Publish<T>(this ISendOnlyBus bus)
        {
            bus.Publish<T>(_=>{},new PublishOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static void Publish<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            bus.Publish(messageConstructor,new PublishOptions());
        }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="message">The message to send.</param>
        public static void Send(this ISendOnlyBus bus, object message)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(message, "message");

            bus.Send(message, new SendOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <remarks>
        /// The message will be sent to the destination configured for T
        /// </remarks>
        public static void Send<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            bus.Send(messageConstructor, new SendOptions());
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="destination">The address of the destination to which the message will be sent.</param>
        /// <param name="message">The message to send.</param>
        public static void Send(this ISendOnlyBus bus, string destination, object message)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(message, "message");

            var options = new SendOptions();

            options.SetDestination(destination);

            bus.Send(message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static void Send<T>(this ISendOnlyBus bus, string destination, Action<T> messageConstructor)
        {
            Guard.AgainstNull(bus, "bus");
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            var options = new SendOptions();

            options.SetDestination(destination);

            bus.Send(messageConstructor, options);
        }
    }
}