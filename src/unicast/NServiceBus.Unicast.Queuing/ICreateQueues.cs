namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Abstraction of the capability to create queues
    /// </summary>
    public interface ICreateQueues
    {
        /// <summary>
        /// Create a messages queue where its name is the address parameter, for the given account.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="account"></param>
        /// <param name="volatileQueues">If true create a volatile (non-durable) queue</param>
        void CreateQueueIfNecessary(Address address, string account, bool volatileQueues = false);
    }
}