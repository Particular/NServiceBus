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
            Guard.AgainstNull(nameof(options), options);
            options.MessageOperationContext.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Returns whether <see cref="DoNotEnforceBestPractices(ExtendableOptions)" /> has ben called or not.
        /// </summary>
        /// <returns><c>true</c> if best practice enforcement has ben disabled, <c>false</c> otherwise.</returns>
        public static bool IgnoredBestPractices(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);
            options.MessageOperationContext.TryGet(out EnforceBestPracticesOptions bestPracticesOptions);
            return !(bestPracticesOptions?.Enabled ?? true);
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this IOutgoingReplyContext context)
        {
            Guard.AgainstNull(nameof(context), context);
            context.GetMessageOperationExtensions().SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this IOutgoingSendContext context)
        {
            Guard.AgainstNull(nameof(context), context);
            context.GetMessageOperationExtensions().SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this ISubscribeContext context)
        {
            Guard.AgainstNull(nameof(context), context);
            context.GetMessageOperationExtensions().SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this IOutgoingPublishContext context)
        {
            Guard.AgainstNull(nameof(context), context);
            context.GetMessageOperationExtensions().SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this IUnsubscribeContext context)
        {
            Guard.AgainstNull(nameof(context), context);
            context.GetMessageOperationExtensions().SetDoNotEnforceBestPractices();
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