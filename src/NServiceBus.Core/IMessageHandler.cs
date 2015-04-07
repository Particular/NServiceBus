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
        void Return<T>(T errorEnum);

        void Reply(object message);

        ICallback Send(string destination, object message);

        void DoNotContinueDispatchingCurrentMessageToHandlers();
        void HandleCurrentMessageLater();
        void SendLocal(object message);
    }

    internal class CommandContext : ICommandContext
    {
        IBus bus;

        public CommandContext(IBus bus)
        {
            this.bus = bus;
        }

        public void Return<T>(T errorEnum)
        {
            bus.Return(errorEnum);
        }

        public void Reply(object message)
        {
            bus.Reply(message);
        }

        public ICallback Send(string destination, object message)
        {
            return bus.Send(destination, message);
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            bus.DoNotContinueDispatchingCurrentMessageToHandlers();
        }

        public void HandleCurrentMessageLater()
        {
            bus.HandleCurrentMessageLater();
        }

        public void SendLocal(object message)
        {
            bus.SendLocal(message);
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
