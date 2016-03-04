namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    /// Timeout persister contract.
    /// </summary>
    public interface IPersistTimeouts
    {
        /// <summary>
        /// Adds a new timeout.
        /// </summary>
        /// <param name="timeout">Timeout data.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Add(TimeoutData timeout, ContextBag context);

        /// <summary>
        /// Removes the timeout if it hasn't been previously removed.
        /// </summary>
        /// <param name="timeoutId">The timeout id to remove.</param>
        /// <param name="context">The current pipeline context.</param>
        /// <returns><c>true</c> when the timeout has successfully been removed by this method call, <c>false</c> otherwise.</returns>
        Task<bool> TryRemove(string timeoutId, ContextBag context);

        /// <summary>
        /// Returns the timeout with the given id from the storage. The timeout will remain in the storage.
        /// </summary>
        /// <param name="timeoutId">The id of the timeout to fetch.</param>
        /// <param name="context">The current pipeline context.</param>
        /// <returns><see cref="TimeoutData" /> with the given id if present in the storage or <c>null</c> otherwise.</returns>
        Task<TimeoutData> Peek(string timeoutId, ContextBag context);

        /// <summary>
        /// Removes the timeouts by saga id.
        /// </summary>
        /// <param name="sagaId">The saga id of the timeouts to remove.</param>
        /// <param name="context">The current pipeline context.</param>
        Task RemoveTimeoutBy(Guid sagaId, ContextBag context);
    }
}