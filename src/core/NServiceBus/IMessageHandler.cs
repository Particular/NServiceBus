namespace NServiceBus
{
	/// <summary>
	/// Defines a message handler.
	/// </summary>
	/// <typeparam name="T">The type of message to be handled.</typeparam>
    public interface IMessageHandler<T> where T : IMessage
    {
		/// <summary>
		/// Handles a message.
		/// </summary>
		/// <param name="message">The message to handle.</param>
		/// <remarks>
		/// This method will be called when a message arrives on the bus and should contain
		/// the custom logic to execute when the message is received.</remarks>
        void Handle(T message);
    }

    /// <summary>
    /// Implement this class to be called when messages of the given type arrive at your endpoint.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHandleMessages<T> : IMessageHandler<T> where T : IMessage {}
}
