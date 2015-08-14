namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Syntactic sugar for <see cref="ISendOnlyBus"/>.
    /// </summary>
    public static class ISendOnlyBusExtensions
    {
       
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="message">The message to publish.</param>
        public static Task Publish(this ISendOnlyBus bus, object message)
        {
            return bus.Publish(message, new PublishOptions());
        }

       
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <typeparam name="T">The message type.</typeparam>
        public static Task Publish<T>(this ISendOnlyBus bus)
        {
            return bus.Publish<T>(_=>{},new PublishOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task Publish<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            return bus.Publish(messageConstructor,new PublishOptions());
        }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="message">The message to send.</param>
        public static Task Send(this ISendOnlyBus bus, object message)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNull("message", message);

            return bus.Send(message, new SendOptions());
        }

        /// <summary>
        /// Instantiates a message of <typeparamref name="T"/> and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <remarks>
        /// The message will be sent to the destination configured for <typeparamref name="T"/>.
        /// </remarks>
        public static Task Send<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNull("messageConstructor", messageConstructor);

            return bus.Send(messageConstructor, new SendOptions());
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="destination">The address of the destination to which the message will be sent.</param>
        /// <param name="message">The message to send.</param>
        public static Task Send(this ISendOnlyBus bus, string destination, object message)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNullAndEmpty("destination", destination);
            Guard.AgainstNull("message", message);

            var options = new SendOptions();

            options.SetDestination(destination);

            return bus.Send(message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task Send<T>(this ISendOnlyBus bus, string destination, Action<T> messageConstructor)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNullAndEmpty("destination", destination);
            Guard.AgainstNull("messageConstructor", messageConstructor);

            var options = new SendOptions();

            options.SetDestination(destination);

            return bus.Send(messageConstructor, options);
        }
    }
}