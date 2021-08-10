namespace NServiceBus.Extensibility
{
    /// <summary>
    /// Context bag which is readonly.
    /// </summary>
    public interface IReadOnlyContextBag
    {
        /// <summary>
        /// Retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve. The fully qualified name of the type will be used to look up the instance.</typeparam>
        /// <returns>The type instance.</returns>
        T Get<T>();

        /// <summary>
        /// Retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="key">The key of the value being looked up.</param>
        /// <returns>The type instance.</returns>
        T Get<T>(string key);

        /// <summary>
        /// Tries to retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        bool TryGet<T>(out T result);

        /// <summary>
        /// Tries to retrieves the specified type from the context.
        /// </summary>
        /// <typeparam name="T">The type to retrieve.</typeparam>
        /// <param name="key">The key of the value being looked up.</param>
        /// <param name="result">The type instance.</param>
        /// <returns><code>true</code> if found, otherwise <code>false</code>.</returns>
        bool TryGet<T>(string key, out T result);
    }
}
