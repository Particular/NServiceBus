namespace NServiceBus.Transports
{
    /// <summary>
    /// Allows fine grained control on how messages are audited
    /// </summary>
    public interface IAuditMessages
    {
        /// <summary>
        /// Called when a message should be sent to audit
        /// </summary>
        /// <param name="sendOptions">The send options of the message</param>
        /// <param name="message">The actual message</param>
        void Audit(OutgoingMessage message,TransportSendOptions sendOptions);
    }
}