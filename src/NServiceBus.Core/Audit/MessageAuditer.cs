namespace NServiceBus.Audit
{
    using System;
    using System.ComponentModel;
    using Features;
    using Transports;

    /// <summary>
    /// This class is used to forward messages to the configured audit queue, reverting the body to 
    /// its original state if needed etc before forwarding the message to the audit queue. It uses
    /// <see cref="ISendMessages"/> which will be injected by the bus.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class MessageAuditer
    {
        /// <summary>
        /// This will be used to forward messages to the specified audit queue.
        /// </summary>
        public ISendMessages MessageSender { get; set; }

        /// <summary>
        /// The TTR to set on forwarded messages. 
        /// </summary>
        public TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }


        /// <summary>
        /// <see cref="Address"/> where the messages needs to be forwarded when the auditing feature is turned on
        /// </summary>
        public Address AuditQueue { get; set; }

        /// <summary>
        /// Indicates if the current endpoint has a valid license
        /// </summary>
        public bool LicenseExpired { get; set; }

        /// <summary>
        /// If the auditing feature is turned on, forward the given transport to the configured audit queue.
        /// </summary>
        public virtual void ForwardMessageToAuditQueue(TransportMessage transportMessage)
        {
            if (!Feature.IsEnabled<Audit>())
            {
                return;
            }

            transportMessage.Headers[Headers.HasLicenseExpired] = LicenseExpired.ToString().ToLower();
            MessageSender.ForwardMessage(transportMessage, TimeToBeReceivedOnForwardedMessages, AuditQueue);
        }
    }
}
