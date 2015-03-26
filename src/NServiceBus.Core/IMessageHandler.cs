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
    public interface IConsumeEvent<T>
    {
        void Handle(T message, IConsumeEventContext context);
    }

    public interface IConsumeEventContext { }

    internal class ConsumeEventContext : IConsumeEventContext
    {
    }

    public interface IConsumeMessage<T>
    {
        void Handle(T message, IConsumeMessageContext messageContext);
    }

    public interface IConsumeMessageContext { }

    internal class ConsumeMessageContext : IConsumeMessageContext
    {
    }
#pragma warning restore 1591
}
