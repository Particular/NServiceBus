namespace NServiceBus
{
    /// <summary>
	/// Defines a message handler.
	/// </summary>
	/// <typeparam name="T">The type of message to be handled.</typeparam>
    public interface IHandleMessages<T>
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

#pragma warning disable 1591
    public interface ISubscribe<T>
    {
        void Handle(T message, ISubscribeContext context);
    }

    public interface ISubscribeContext { }

    internal class SubscribeContext : ISubscribeContext
    {
    }

    public interface IHandle<T>
    {
        void Handle(T message, IHandleContext context);
    }

    public interface IHandleContext { }

    internal class HandleContext : IHandleContext
    {
    }
#pragma warning restore 1591
}
