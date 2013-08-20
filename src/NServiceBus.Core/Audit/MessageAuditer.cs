namespace NServiceBus.Audit
{
    using System;
    using Features;
    using Transports;
    using Unicast.Queuing;

    /// <summary>
    /// This class is used to forward messages to the configured audit queue, reverting the body to 
    /// its original state if needed etc before forwarding the message to the audit queue. It uses
    /// the ISendMessages which will be injected by the bus.
    /// </summary>
    public class MessageAuditer 
    {
        /// <summary>
        /// This will be used to forward messages to the specified audit queue.
        /// </summary>
        public ISendMessages MessageForwarder { get; set; }

        /// <summary>
        /// The TTR to set on forwarded messages. 
        /// </summary>
        public TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }
        

        /// <summary>
        /// address where the messages needs to be forwarded when the auditing feature is turned on
        /// </summary>
        public Address AuditQueue { get; set; }
        

        /// <summary>
        /// If the auditing feature is turned on, forward the given transport to the configured audit queue.
        /// </summary>
        /// <param name="transportMessage"></param>
        public void ForwardMessageToAuditQueue(TransportMessage transportMessage)
        {
            if (!Feature.IsEnabled<Audit>()) return;
            
            // Rever the original body if needed (if any mutators were applied, forward the original body as received)
            transportMessage.RevertToOriginalBodyIfNeeded();

            // Create a new transport message which will contain the appropriate headers
            var messageToForward = new TransportMessage(transportMessage.Id, transportMessage.Headers)
            {
                Body = transportMessage.Body,
                CorrelationId = transportMessage.CorrelationId,
                MessageIntent = transportMessage.MessageIntent,
                Recoverable = transportMessage.Recoverable,
                ReplyToAddress = Address.Local,
                TimeToBeReceived = TimeToBeReceivedOnForwardedMessages == TimeSpan.Zero ? transportMessage.TimeToBeReceived : TimeToBeReceivedOnForwardedMessages
            };
            if (transportMessage.ReplyToAddress != null)
                messageToForward.Headers[Headers.OriginatingAddress] = transportMessage.ReplyToAddress.ToString();

            // Send the newly created transport message to the configured audit queue
            MessageForwarder.Send(messageToForward, AuditQueue);            
        }

        public Address Address
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsDisabled
        {
            get { throw new NotImplementedException(); }
        }
    }
}
