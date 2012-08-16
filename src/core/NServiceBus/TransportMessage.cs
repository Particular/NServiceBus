using System;
using System.Collections.Generic;

namespace NServiceBus
{
	/// <summary>
	/// An envelope used by NServiceBus to package messages for transmission.
	/// </summary>
	/// <remarks>
	/// All messages sent and received by NServiceBus are wrapped in this class. 
	/// More than one message can be bundled in the envelope to be transmitted or 
	/// received by the bus.
	/// </remarks>
    [Serializable]
    public class TransportMessage
    {
		/// <summary>
		/// Gets/sets the identifier of this message bundle.
		/// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets/sets the identifier that is copied to <see cref="CorrelationId"/>.
        /// </summary>
        public string IdForCorrelation { get; set; }

		/// <summary>
		/// Gets/sets the uniqe identifier of another message bundle
		/// this message bundle is associated with.
		/// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Gets/sets the reply-to address of the message bundle - replaces 'ReturnAddress'.
        /// </summary>
        public Address ReplyToAddress { get; set; }

		/// <summary>
		/// Gets/sets whether or not the message is supposed to
		/// be guaranteed deliverable.
		/// </summary>
        public bool Recoverable { get; set; }

        /// <summary>
        /// Indicates to the infrastructure the message intent (publish, or regular send).
        /// </summary>
        public MessageIntentEnum MessageIntent { get; set; }

        private TimeSpan timeToBeReceived = TimeSpan.MaxValue;

		/// <summary>
		/// Gets/sets the maximum time limit in which the message bundle
		/// must be received.
		/// </summary>
        public TimeSpan TimeToBeReceived
        {
            get { return timeToBeReceived; }
            set { timeToBeReceived = value; }
        }

        /// <summary>
        /// Gets/sets other applicative out-of-band information.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

		/// <summary>
		/// Gets/sets a byte array to the body content of the message
		/// </summary>
        public byte[] Body { get; set; }
    }
}
