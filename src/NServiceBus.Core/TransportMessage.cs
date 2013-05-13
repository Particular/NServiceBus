using System;
using System.Collections.Generic;

namespace NServiceBus
{
    using IdGeneration;
    using Support;

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
        /// Initalizes the transport message with a CombGuid as identifier
        /// </summary>
        public TransportMessage()
        {
            Id = CombGuid.Generate().ToString();
            CorrelationId = Id;
            Headers.Add(NServiceBus.Headers.OriginatingEndpoint, Configure.EndpointName);
            Headers.Add(NServiceBus.Headers.OriginatingMachine, RuntimeEnvironment.MachineName);
            MessageIntent = MessageIntentEnum.Send;
        }

        /// <summary>
        /// Gets/sets the identifier of this message bundle.
        /// </summary>
        public string Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                Headers["CorrId"] = id;
            }
        }

        string id;

        /// <summary>
        /// Use this method to change the stable ID of the given message.
        /// </summary>
        /// <param name="newId"></param>
        public void ChangeMessageId(string newId)
        {
            Id = newId;
            CorrelationId = newId;
        }

        /// <summary>
        /// Gets/sets the identifier that is copied to <see cref="CorrelationId"/>.
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0", Replacement = "Id")]
        public string IdForCorrelation
        {
            get
            {
                return Id;
            }
        }

        /// <summary>
        /// Gets/sets the unique identifier of another message bundle
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
        public MessageIntentEnum MessageIntent
        {
            get
            {
                MessageIntentEnum messageIntent = default(MessageIntentEnum);

                if (Headers.ContainsKey(NServiceBus.Headers.MessageIntent))
                {
                    MessageIntentEnum.TryParse(Headers[NServiceBus.Headers.MessageIntent], true, out messageIntent);
                }

                return messageIntent;
            }
            set { Headers[NServiceBus.Headers.MessageIntent] = value.ToString(); }
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

        /// <summary>
        /// Gets/sets other applicative out-of-band information.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get { return headers ?? (headers = new Dictionary<string, string>()); }
            set { headers = value; }
        }


        Dictionary<string, string> headers;

        /// <summary>
        /// Gets/sets a byte array to the body content of the message
        /// </summary>
        public byte[] Body { get; set; }
    }
}
