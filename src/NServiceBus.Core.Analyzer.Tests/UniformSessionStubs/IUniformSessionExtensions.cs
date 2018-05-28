namespace NServiceBus.UniformSession
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for <see cref="IUniformSession" />.
    /// </summary>
    public static class IUniformSessionExtensions
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="session">The instance of <see cref="IUniformSession" /> to use for the action.</param>
        /// <param name="message">The message to send.</param>
        public static Task Send(this IUniformSession session, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of <typeparamref name="T" /> and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="session">The instance of <see cref="IUniformSession" /> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <remarks>
        /// The message will be sent to the destination configured for <typeparamref name="T" />.
        /// </remarks>
        public static Task Send<T>(this IUniformSession session, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="session">The instance of <see cref="IUniformSession" /> to use for the action.</param>
        /// <param name="destination">The address of the destination to which the message will be sent.</param>
        /// <param name="message">The message to send.</param>
        public static Task Send(this IUniformSession session, string destination, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="session">The instance of <see cref="IUniformSession" /> to use for the action.</param>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task Send<T>(this IUniformSession session, string destination, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends the message back to the current endpoint.
        /// </summary>
        /// <param name="session">Object being extended.</param>
        /// <param name="message">The message to send.</param>
        public static Task SendLocal(this IUniformSession session, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current endpoint.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="session">Object being extended.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task SendLocal<T>(this IUniformSession session, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="session">The instance of <see cref="IUniformSession" /> to use for the action.</param>
        /// <param name="message">The message to publish.</param>
        public static Task Publish(this IUniformSession session, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="session">The instance of <see cref="IUniformSession" /> to use for the action.</param>
        /// <typeparam name="T">The message type.</typeparam>
        public static Task Publish<T>(this IUniformSession session)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="session">The instance of <see cref="IUniformSession" /> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static Task Publish<T>(this IUniformSession session, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }
    }
}