namespace NServiceBus.Transports
{
    using Unicast;

    public interface IAuditMessages
    {
        /// <summary>
        /// Sends the given <paramref name="message"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sendOptions"></param>
        void Audit(SendOptions sendOptions,TransportMessage message);
    }
}