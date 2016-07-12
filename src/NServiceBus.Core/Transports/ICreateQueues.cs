namespace NServiceBus.Transport
{
    using System.Threading.Tasks;

    /// <summary>
    /// Abstraction of the capability to create queues.
    /// </summary>
    public interface ICreateQueues
    {
        /// <summary>
        /// Creates message queues for the defined queue bindings and identity.
        /// </summary>
        Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity);
    }
}