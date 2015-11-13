namespace NServiceBus.Transports
{
    using System.Threading.Tasks;

    /// <summary>
    /// Abstraction of the capability to create queues.
    /// </summary>
    public interface ICreateQueues
    {
        /// <summary>
        /// Create a messages queue where its name is the address parameter, for the given account.
        /// </summary>
        /// <remarks>This method will be executed in parallel if multiple addresses need to be created.</remarks>
        Task CreateQueueIfNecessary(string address, string account);
    }
}