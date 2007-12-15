using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace NServiceBus.Unicast.Transport
{
	//TODO: Would the name of this be clearer as something else such as Envelope or TransportMessage?
    //Possibly - preferred similarity to the way WCF & MSMQ expose their Message class.
	/// <summary>
	/// An envelope used by NServiceBus to package messages for transmission.
	/// </summary>
	/// <remarks>
	/// All messages sent and received by NServiceBus are wrapped in this class. 
	/// More than one message can be bundled in the envelope to be transmitted or 
	/// received by the bus.
	/// </remarks>
    [Serializable]
    public class Msg
    {
        private string id;

		/// <summary>
		/// Gets/sets the identifier of this message bundle.
		/// </summary>
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        private string correlationId;

		/// <summary>
		/// Gets/sets the uniqe identifier of another message bundle
		/// this message bundle is associated with.
		/// </summary>
        public string CorrelationId
        {
            get { return correlationId; }
            set { correlationId = value; }
        }

        private string returnAddress;

		/// <summary>
		/// Gets/sets the return address of the message bundle.
		/// </summary>
        public string ReturnAddress
        {
            get { return returnAddress; }
            set { returnAddress = value; }
        }

        private string windowsIdentityName;

		/// <summary>
		/// Gets/sets the name of the Windows identity the message
		/// is being sent as.
		/// </summary>
        public string WindowsIdentityName
        {
            get { return windowsIdentityName; }
            set { windowsIdentityName = value; }
        }

        private bool recoverable;

		/// <summary>
		/// Gets/sets whether or not the message is supposed to
		/// be guaranteed deliverable.
		/// </summary>
        public bool Recoverable
        {
            get { return recoverable; }
            set { recoverable = value; }
        }

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

        private IMessage[] body;

		/// <summary>
		/// Gets/sets the array of messages in the message bundle.
		/// </summary>
		/// <remarks>
		/// Since the XmlSerializer doesn't work well with interfaces,
		/// we ask it to ignore this data and synchronize with the <see cref="messages"/> field.
		/// </remarks>
        [XmlIgnore]
        public IMessage[] Body
        {
            get { return body; }
            set { body = value; messages = new List<object>(body); }
        }

        private Stream bodyStream;

        
		/// <summary>
		/// Gets/sets a stream to the body content of the message
		/// </summary>
		/// <remarks>
		/// Used for cases where we can't deserialize the contents.
		/// </remarks>
		[XmlIgnore]
        public Stream BodyStream
        {
            get { return bodyStream; }
            set { bodyStream = value; }
        }

        private List<object> messages;

		/// <summary>
		/// Gets/sets the list of messages in the message bundle.
		/// </summary>
        public List<object> Messages
        {
            get { return messages; }
            set { messages = value; }
        }

		/// <summary>
		/// Recreates the list of messages in the body field
		/// from the contents of the messages field.
		/// </summary>
        public void CopyMessagesToBody()
        {
            this.body = new IMessage[this.messages.Count];
            this.messages.CopyTo(this.body);
        }
    }
}
