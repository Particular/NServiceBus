namespace NServiceBus.Extensibility
{
    /// <summary>
    /// Context bag which is readonly.
    /// </summary>
    public interface ReadonlyContextBag
    {
        /// <summary>
        /// Retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <returns>The type instance.</returns>
        T Get<T>();

        /// <summary>
        /// Tries to retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        bool TryGet<T>(out T result);
    }
}
