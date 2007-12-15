using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus
{
	/// <summary>
	/// A base class from which message handlers can be derived.
	/// </summary>
	/// <typeparam name="T">The type of message the message handler will handle.</typeparam>
    public abstract class BaseMessageHandler<T> : IMessageHandler<T> where T : IMessage
    {
		/// <summary>
		/// Handles a message.
		/// </summary>
		/// <param name="message">The message to handle.</param>
		/// <remarks>
		/// This method will be called when a message arrives on the bus and should contain
		/// the custom logic to execute when the message is received.</remarks>
        public abstract void Handle(T message);

        private IBus bus;

		/// <summary>
		/// Gets/sets the <see cref="IBus"/> implementation associated with the
		/// handler.
		/// </summary>
		/// <remarks>
		/// The Bus can be used for sending or replying to messages or sending return codes.
		/// </remarks>
        public IBus Bus { get { return this.bus; } set { this.bus = value; } }
    }
}
