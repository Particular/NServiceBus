namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Implementers signal their wish to create a queue, regardless of technology (e.g. MSMQ or SQL Server).
    /// </summary>
    public interface IWantQueueCreated
    {
        /// <summary>
        /// Address of queue the implementer requires.
        /// </summary>
        Address Address { get; }
        
        /// <summary>
        /// True if no need to create queue
        /// </summary>
        bool ShouldCreateQueue(Configure config);
    }
}
