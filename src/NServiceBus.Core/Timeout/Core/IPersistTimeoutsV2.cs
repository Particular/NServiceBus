namespace NServiceBus.Timeout.Core
{
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
        void Remove(string timeoutId);
    }
}