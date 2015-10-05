namespace NServiceBus.Outbox
{
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Implemented by the persisters to provide outbox storage capabilities.
    /// </summary>
    public interface IOutboxStorage
    {
        /// <summary>
        /// Tries to find the given message in the outbox.
        /// </summary>
        /// <returns>If there is no <see cref="OutboxMessage"/> present for the given <paramref name="messageId"/> then null is returned.</returns>
        Task<OutboxMessage> Get(string messageId, ReadOnlyContextBag options);

        /// <summary>
        /// Stores the outbox message to enable deduplication an re-dispatching of related transport operations.
        /// </summary>
        Task Store(OutboxMessage message, OutboxTransaction transaction, ReadOnlyContextBag options);

        /// <summary>
        /// Tells the storage that the message has been dispatched and its now safe to clean up the transport operations.
        /// </summary>
        Task SetAsDispatched(string messageId, ReadOnlyContextBag options);

        /// <summary>
        /// Creates the <see cref="OutboxTransaction"/>.
        /// </summary>
        /// <param name="context">The current pipeline contex.</param>
        /// <returns>The created outbox transaction.</returns>
        Task<OutboxTransaction> BeginTransaction(ReadOnlyContextBag context);
    }
}