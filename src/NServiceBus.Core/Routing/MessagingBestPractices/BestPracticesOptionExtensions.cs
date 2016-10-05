namespace NServiceBus
{
    using Extensibility;
    using Pipeline;

    /// <summary>
    /// Provides options for disabling the best practice enforcement.
    /// </summary>
    public static class BestPracticesOptionExtensions
    {
        /// <summary>
        /// Turns off the best practice enforcement for the given message.
        /// </summary>
        public static void DoNotEnforceBestPractices(this ExtendableOptions options)
        {
            options.Context.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Returns whether <see cref="DoNotEnforceBestPractices(NServiceBus.Extensibility.ExtendableOptions)" /> has ben called or
        /// not.
        /// </summary>
        /// <returns><c>true</c> if best practice enforcement has ben disabled, <c>false</c> otherwise.</returns>
        public static bool IgnoredBestPractices(this ExtendableOptions options)
        {
            EnforceBestPracticesOptions bestPracticesOptions;
            options.Context.TryGet(out bestPracticesOptions);
            return !(bestPracticesOptions?.Enabled ?? true);
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this IOutgoingReplyContext context)
        {
            context.Extensions.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this IOutgoingSendContext context)
        {
            context.Extensions.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this ISubscribeContext context)
        {
            context.Extensions.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this IOutgoingPublishContext context)
        {
            context.Extensions.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this IUnsubscribeContext context)
        {
            context.Extensions.SetDoNotEnforceBestPractices();
        }

        static void SetDoNotEnforceBestPractices(this ContextBag context)
        {
            var bestPracticesOptions = new EnforceBestPracticesOptions
            {
                Enabled = false
            };
            context.Set(bestPracticesOptions);
        }
    }
}