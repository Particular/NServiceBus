namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     An envelope used by NServiceBus to package messages for transmission.
    /// </summary>
    /// <remarks>
    ///     All messages sent and received by NServiceBus are wrapped in this class.
    ///     More than one message can be bundled in the envelope to be transmitted or
    ///     received by the bus.
    /// </remarks>
    [Serializable]
    public class TransportMessage
    {
        /// <summary>
        ///     Initializes the transport message with a CombGuid as identifier
        /// </summary>
        public TransportMessage()
        {
            id = CombGuid.Generate().ToString();
            Headers[NServiceBus.Headers.MessageId] = id;
            CorrelationId = id;
            MessageIntent = MessageIntentEnum.Send;
            Headers[NServiceBus.Headers.NServiceBusVersion] = GitFlowVersion.MajorMinorPatch;
            Headers[NServiceBus.Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
        }


        /// <summary>
        ///     Creates a new TransportMessage with the given id and headers
        /// </summary>
        public TransportMessage(string existingId, Dictionary<string, string> existingHeaders,Address replyToAddress = null)
        {
            ReplyToAddress = replyToAddress;

            if (existingHeaders == null)
            {
                existingHeaders = new Dictionary<string, string>();
            }

            headers = existingHeaders;
            id = existingId;

            //only update the "stable id" if there isn't one present already
            if (!Headers.ContainsKey(NServiceBus.Headers.MessageId))
            {
                Headers[NServiceBus.Headers.MessageId] = existingId;
            }
        }

        /// <summary>
        /// Creates a new transport message with the given headers and body
        /// </summary>
        /// <param name="existingHeaders">The existing headers</param>
        /// <param name="body">The body, can be byte[0] for control messages</param>
        public TransportMessage(Dictionary<string, string> existingHeaders,byte[] body)
        {
            headers = existingHeaders;
            this.body = body;
            Recoverable = true;
        }

        /// <summary>
        ///     Gets/sets the identifier of this message bundle.
        /// </summary>
        public string Id
        {
            get
            {
                string headerId;
                if (Headers.TryGetValue(NServiceBus.Headers.MessageId, out headerId))
                {
                    return headerId;
                }
                return id;
            }
        }

        /// <summary>
        ///     Gets/sets the unique identifier of another message bundle
        ///     this message bundle is associated with.
        /// </summary>
        public string CorrelationId
        {
            get
            {
                string correlationId;

                if (Headers.TryGetValue(NServiceBus.Headers.CorrelationId, out correlationId))
                {
                    return correlationId;
                }

                return null;
            }
            set { Headers[NServiceBus.Headers.CorrelationId] = value; }
        }

        /// <summary>
        ///     Gets/sets the reply-to address of the message bundle - replaces 'ReturnAddress'.
        /// </summary>
        public Address ReplyToAddress { get; private set; }

        /// <summary>
        ///     Gets/sets whether or not the message is supposed to
        ///     be guaranteed deliverable.
        /// </summary>
        public bool Recoverable { get; set; }

        /// <summary>
        ///     Indicates to the infrastructure the message intent (publish, or regular send).
        /// </summary>
        public MessageIntentEnum MessageIntent
        {
            get
            {
                var messageIntent = default(MessageIntentEnum);

                string messageIntentString;
                if (Headers.TryGetValue(NServiceBus.Headers.MessageIntent, out messageIntentString))
                {
                    Enum.TryParse(messageIntentString, true, out messageIntent);
                }

                return messageIntent;
            }
            set { Headers[NServiceBus.Headers.MessageIntent] = value.ToString(); }
        }


        /// <summary>
        ///     Gets/sets the maximum time limit in which the message bundle
        ///     must be received.
        /// </summary>
        public TimeSpan TimeToBeReceived
        {
            get { return timeToBeReceived; }
            set { timeToBeReceived = value; }
        }

        /// <summary>
        ///     Gets/sets other applicative out-of-band information.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get { return headers; }
        }


        /// <summary>
        ///     Gets/sets a byte array to the body content of the message
        /// </summary>
        public byte[] Body
        {
            get { return body; }
            set { UpdateBody(value); }
        }

        /// <summary>
        ///     Use this method to change the stable ID of the given message.
        /// </summary>
        internal void ChangeMessageId(string newId)
        {
            id = newId;
            CorrelationId = newId;
        }

        /// <summary>
        ///     Use this method to update the body if this message
        /// </summary>
        void UpdateBody(byte[] updatedBody)
        {
            //preserve the original body if needed
            if (body != null && originalBody == null)
            {
                originalBody = new byte[body.Length];
                Buffer.BlockCopy(body, 0, originalBody, 0, body.Length);
            }

            body = updatedBody;
        }

        /// <summary>
        ///     Makes sure that the body is reset to the exact state as it was when the message was created
        /// </summary>
        internal void RevertToOriginalBodyIfNeeded()
        {
            if (originalBody != null)
            {
                body = originalBody;
            }
        }

        readonly Dictionary<string, string> headers = new Dictionary<string, string>();

        byte[] body;
        string id;
        byte[] originalBody;
        TimeSpan timeToBeReceived = TimeSpan.MaxValue;
    }
}