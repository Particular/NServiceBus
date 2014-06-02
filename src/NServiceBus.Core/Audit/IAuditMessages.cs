namespace NServiceBus.Transports
{
    using Unicast;

    public interface IAuditMessages
    {
        /// <summary>
        /// Sends the given <paramref name="message"/>
        /// </summary>
        void Audit(SendOptions sendOptions,TransportMessage message);
    }
}