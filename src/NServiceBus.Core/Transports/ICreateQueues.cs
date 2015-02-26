namespace NServiceBus.Transports
{
    /// <summary>
    /// Abstraction of the capability to create queues
    /// </summary>
    public interface ICreateQueues
    {
        /// <summary>
        /// Create a messages queue where its name is the address parameter, for the given account.
        /// </summary>
        void CreateQueueIfNecessary(string address, string account);
    }
}