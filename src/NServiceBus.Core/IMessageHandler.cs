namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
	/// Defines a message handler.
	/// </summary>
	/// <typeparam name="T">The type of message to be handled.</typeparam>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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
