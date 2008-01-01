using System;
using System.Collections.Generic;

namespace NServiceBus.Unicast.Transport
{
	/// <summary>
	/// Defines the basic functionality of a transport to be used by NServiceBus.
	/// </summary>
    public interface ITransport : IDisposable
    {
		/// <summary>
		/// Starts the transport.
		/// </summary>
        void Start();

		/// <summary>
		/// Sets the list of message types to be received by the transport.
		/// </summary>
        IList<Type> MessageTypesToBeReceived { set; }

		/// <summary>
		/// Sends a message to the specified destination.
		/// </summary>
		/// <param name="m">The message to send.</param>
		/// <param name="destination">The address to send the message to.</param>
        void Send(Msg m, string destination);

		/// <summary>
		/// Re-queues a message for processing at another time.
		/// </summary>
		/// <param name="m">The message to process later.</param>
        void ReceiveMessageLater(Msg m);

		/// <summary>
		/// Gets the address at which the transport receives messages.
		/// </summary>
        string Address { get; }

		/// <summary>
		/// Raised when a message is received at the transport's <see cref="Address"/>.
		/// </summary>
        event EventHandler<MsgReceivedEventArgs> MsgReceived;
    }

	/// <summary>
	/// Defines the arguments passed to the event handler of the
	/// <see cref="ITransport.MsgReceived"/> event.
	/// </summary>
    public class MsgReceivedEventArgs : EventArgs
    {
		/// <summary>
		/// Initializes a new MsgReceivedEventArgs.
		/// </summary>
		/// <param name="m">The message that was received.</param>
        public MsgReceivedEventArgs(Msg m)
        {
            this.message = m;
        }

        private readonly Msg message;

		/// <summary>
		/// Gets the message received.
		/// </summary>
        public Msg Message
        {
            get { return message; }
        }
    }
}
