namespace NServiceBus.Outbox
{
    using System.Threading.Tasks;

    /// <summary>
    /// Implemented by the persisters to provide outbox storage capabilities.
    /// </summary>
    public interface IOutboxStorage
    {
        /// <summary>
        /// Tries to find the given message in the outbox.
        /// </summary>
        /// <returns>If there is no <see cref="OutboxMessage"/> present for the given <paramref name="messageId"/> then null is returned.</returns>
        Task<OutboxMessage> Get(string messageId, OutboxStorageOptions options);

        /// <summary>
        /// Stores.
        /// </summary>
        Task Store(OutboxMessage message, OutboxStorageOptions options);

        /// <summary>
        /// Tells the storage that the message has been dispatched and its now safe to clean up the transport operations.
        /// </summary>
        Task SetAsDispatched(string messageId, OutboxStorageOptions options);
    }
}