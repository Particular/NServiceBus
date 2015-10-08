namespace NServiceBus.Timeout.Core
{
    /// <summary>
    /// Timeout persister contract.
    /// </summary>
    public interface IPersistTimeoutsV2
    {
        /// <summary>
        /// Reads timeout data.
        /// </summary>
        /// <param name="timeoutId">The timeout id to read.</param>
        /// <returns><see cref="TimeoutData"/> of the timeout if it was found. <c>null</c> otherwise.</returns>
        TimeoutData Peek(string timeoutId);

        /// <summary>
        /// Removes the timeout if it hasn't been previously removed.
        /// </summary>
        /// <param name="timeoutId">The timeout id to remove.</param>
        /// <returns><c>true</c> if the timeout has been successfully removed or <c>false</c> if there was no timeout to remove.</returns>
        bool TryRemove(string timeoutId);
    }
}
