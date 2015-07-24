namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Implementers signal their wish to create a queue, regardless of technology (e.g. MSMQ or SQL Server).
    /// </summary>
    [ObsoleteEx(ReplacementTypeOrMember = "QueueBindings", RemoveInVersion = "7.0", TreatAsErrorFromVersion = "6.0")]
    public interface IWantQueueCreated
    {
        /// <summary>
        /// Address of queue the implementer requires.
        /// </summary>
        string Address { get; }
        
        /// <summary>
        /// True if no need to create queue.
        /// </summary>
        bool ShouldCreateQueue();
    }
}
