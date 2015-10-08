namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;

    /// <summary>
    ///     Timeout persister contract.
    /// </summary>
    public interface IPersistTimeouts
    {
        /// <summary>
        ///     Adds a new timeout.
        /// </summary>
        /// <param name="timeout">Timeout data.</param>
        /// <param name="context">The current pipeline context.</param>
        Task Add(TimeoutData timeout, ReadOnlyContextBag context);

        /// <summary>
        ///     Removes the timeout if it hasn't been previously removed.
        /// </summary>
        /// <param name="timeoutId">The timeout id to remove.</param>
        /// <param name="context">The current pipeline context.</param>
        /// <returns><see cref="TimeoutData" /> of the timeout if it was successfully removed. <c>null</c> otherwise.</returns>
        Task<TimeoutData> Remove(string timeoutId, ReadOnlyContextBag context);

        /// <summary>
        ///     Removes the timeouts by saga id.
        /// </summary>
        /// <param name="sagaId">The saga id of the timeouts to remove.</param>
        /// <param name="context">The current pipeline context.</param>
        Task RemoveTimeoutBy(Guid sagaId, ReadOnlyContextBag context);
    }
}