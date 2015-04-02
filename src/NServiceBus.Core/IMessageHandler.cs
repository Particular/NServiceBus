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
    public interface IProcessEvents<T>
    {
        void Handle(T message, IEventContext context);
    }

    public interface IEventContext { }

    internal class EventContext : IEventContext
    {
    }

    public interface IProcessCommands<T>
    {
        void Handle(T message, ICommandContext context);
    }

    public interface ICommandContext
    {
        void Reply(object message);
    }

    internal class CommandContext : ICommandContext
    {
        IBus bus;

        public CommandContext(IBus bus)
        {
            this.bus = bus;
        }

        public void Reply(object message)
        {
            bus.Reply(message);
        }
    }

    public interface IProcessResponses<T>
    {
        void Handle(T message, IResponseContext context);
    }

    public interface IResponseContext { }

    internal class ResponseContext : IResponseContext
    {
    }
#pragma warning restore 1591
}
