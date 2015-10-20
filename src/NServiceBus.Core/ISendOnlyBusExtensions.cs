namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Syntactic sugar for <see cref="ISendOnlyBus"/>.
    /// </summary>
    public static partial class ISendOnlyBusExtensions
    {
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="message">The message to publish.</param>
        public static Task PublishAsync(this ISendOnlyBus bus, object message)
        {
            return bus.PublishAsync(message, new PublishOptions());
        }


        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <typeparam name="T">The message type.</typeparam>
        public static Task PublishAsync<T>(this ISendOnlyBus bus)
        {
            return bus.PublishAsync<T>(_ => { }, new PublishOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task PublishAsync<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            return bus.PublishAsync(messageConstructor, new PublishOptions());
        }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="message">The message to send.</param>
        public static Task SendAsync(this ISendOnlyBus bus, object message)
        {
            Guard.AgainstNull(nameof(bus), bus);
            Guard.AgainstNull(nameof(message), message);

            return bus.SendAsync(message, new SendOptions());
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
        public static Task SendAsync<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull(nameof(bus), bus);
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);

            return bus.SendAsync(messageConstructor, new SendOptions());
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="destination">The address of the destination to which the message will be sent.</param>
        /// <param name="message">The message to send.</param>
        public static Task SendAsync(this ISendOnlyBus bus, string destination, object message)
        {
            Guard.AgainstNull(nameof(bus), bus);
            Guard.AgainstNullAndEmpty(nameof(destination), destination);
            Guard.AgainstNull(nameof(message), message);

            var options = new SendOptions();

            options.SetDestination(destination);

            return bus.SendAsync(message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task SendAsync<T>(this ISendOnlyBus bus, string destination, Action<T> messageConstructor)
        {
            Guard.AgainstNull(nameof(bus), bus);
            Guard.AgainstNullAndEmpty(nameof(destination), destination);
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);

            var options = new SendOptions();

            options.SetDestination(destination);

            return bus.SendAsync(messageConstructor, options);
        }

        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="message">The message to send.</param>
        public static Task SendLocalAsync(this ISendOnlyBus bus, object message)
        {
            Guard.AgainstNull(nameof(bus), bus);
            Guard.AgainstNull(nameof(message), message);

            var options = new SendOptions();

            options.RouteToLocalEndpointInstance();

            return bus.SendAsync(message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task SendLocalAsync<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull(nameof(bus), bus);
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);

            var options = new SendOptions();

            options.RouteToLocalEndpointInstance();

            return bus.SendAsync(messageConstructor, options);
        }
    }
}