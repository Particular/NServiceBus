namespace NServiceBus.Extensibility
{
    using Pipeline;

    /// <summary>
    /// Provides hidden access to the extension context.
    /// </summary>
    public static class ExtendableOptionsExtensions
    {
        /// <summary>
        /// Gets access to a "bucket", this allows the developer to pass information from extension methods down to behaviors.
        /// </summary>        
        public static ContextBag GetExtensions(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);
            return options.Context;
        }

        /// <summary>
        /// TODO.
        /// </summary>
        public static bool TryGetOperationProperty<T>(this IBehaviorContext behaviorContext, out T value)
        {
            Guard.AgainstNull(nameof(behaviorContext), behaviorContext);

            if (behaviorContext.Extensions.TryGet("NServiceBus.ExtendableOptionsKey", out int prefix))
            {
                return behaviorContext.Extensions.TryGet($"{prefix}:{typeof(T).FullName}", out value);
            }

            return behaviorContext.Extensions.TryGet(out value);
        }

        /// <summary>
        /// TODO.
        /// </summary>
        public static bool TryGetOperationProperty<T>(this IBehaviorContext behaviorContext, string key, out T value)
        {
            Guard.AgainstNull(nameof(behaviorContext), behaviorContext);

            if (behaviorContext.Extensions.TryGet("NServiceBus.ExtendableOptionsKey", out int prefix))
            {
                return behaviorContext.Extensions.TryGet($"{prefix}:{key}", out value);
            }

            return behaviorContext.Extensions.TryGet(key, out value);
        }
    }
}