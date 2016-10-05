namespace NServiceBus
{
    using System;

    /// <summary>
    /// The abstraction for creating interface-based messages.
    /// </summary>
    public interface IMessageCreator
    {
        /// <summary>
        /// Creates an instance of the message type T.
        /// </summary>
        /// <typeparam name="T">The type of message interface to instantiate.</typeparam>
        /// <returns>A message object that implements the interface T.</returns>
        T CreateInstance<T>();

        /// <summary>
        /// Creates an instance of the message type T and fills it with data.
        /// </summary>
        /// <typeparam name="T">The type of message interface to instantiate.</typeparam>
        /// <param name="action">An action to set various properties of the instantiated object.</param>
        /// <returns>A message object that implements the interface T.</returns>
        T CreateInstance<T>(Action<T> action);

        /// <summary>
        /// Creates an instance of the given message type.
        /// </summary>
        /// <param name="messageType">The type of message to instantiate.</param>
        /// <returns>A message object that implements the given interface.</returns>
        object CreateInstance(Type messageType);
    }
}