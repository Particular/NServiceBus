using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace NServiceBus.Unicast.Transport
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
		/// Gets/sets the return address of the message bundle.
		/// </summary>
        public string ReturnAddress { get; set; }

		/// <summary>
		/// Gets/sets the name of the Windows identity the message
		/// is being sent as.
		/// </summary>
        public string WindowsIdentityName { get; set; }

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
        /// Gets/sets the time that the message was sent by the source machine.
        /// </summary>
        public DateTime TimeSent { get; set; }

        /// <summary>
        /// Gets/sets other applicative out-of-band information.
        /// </summary>
        public List<HeaderInfo> Headers { get; set; }

		/// <summary>
		/// Gets/sets a stream to the body content of the message
		/// </summary>
		/// <remarks>
		/// Used for cases where we can't deserialize the contents.
		/// </remarks>
        [XmlIgnore]
        public Stream BodyStream { get; set; }
    }
}
