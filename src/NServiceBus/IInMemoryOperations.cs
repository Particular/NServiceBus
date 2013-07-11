namespace NServiceBus
{
    /// <summary>
    /// In memory operations
    /// </summary>
    public interface IInMemoryOperations
    {
        /// <summary>
        /// Raises an in memory event
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="event">The message to raise</param>
        void Raise<T>(T @event);

        /// <summary>
        /// Instantiates a message of type T and raise it in memory
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageContructor">An action which initializes properties of the message</param>
        void Raise<T>(System.Action<T> messageContructor);
    }
}