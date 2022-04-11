namespace NServiceBus.Extensibility
{
    /// <summary>
    /// Provides extensions for the <see cref="ContextBag"/> class.
    /// </summary>
    public static class ContextBagExtensions
    {
        static string GetPrefixedKey(string prefix, string key) => string.IsNullOrEmpty(prefix) ? key : $"{prefix}:{key}"; // null as the scope key places the value in the global scope.
        static string GetPrefixedKey<T>(string prefix) => GetPrefixedKey(prefix, typeof(T).FullName);

        /// <summary>
        /// Tries to retrieves the specified type from the context in a given scope.
        /// </summary>
        public static bool TryGetScoped<T>(this ContextBag context, string scope, out T result) => context.TryGet(GetPrefixedKey<T>(scope), out result);

        /// <summary>
        /// Tries to retrieves the specified type from the context in a given scope.
        /// </summary>
        public static bool TryGetScoped<T>(this ContextBag context, string scope, string key, out T result) => context.TryGet(GetPrefixedKey(scope, key), out result);

        /// <summary>
        /// Gets the requested value from the specific scope, a new value will be created in the scope if needed.
        /// </summary>
        public static T GetOrCreateScoped<T>(this ContextBag context, string scope) where T : class, new() => context.GetOrCreate<T>(GetPrefixedKey<T>(scope));

        /// <summary>
        /// Stores the type instance in the context in a specific scope.
        /// </summary>
        public static void SetScoped<T>(this ContextBag context, string scope, T value) => context.Set(GetPrefixedKey<T>(scope), value);

        /// <summary>
        /// Stores the type instance in the context in a specific scope.
        /// </summary>
        public static void SetScoped<T>(this ContextBag context, string scope, string key, T value) => context.Set(GetPrefixedKey(scope, key), value);
    }
}