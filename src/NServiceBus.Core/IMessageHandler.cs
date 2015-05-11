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
}
