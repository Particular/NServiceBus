namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Routing;
    using NServiceBus.Routing.MessagingBestPractices;

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
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this OutgoingReplyContext context)
        {
            context.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this OutgoingSendContext context)
        {
            context.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this SubscribeContext context)
        {
            context.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this OutgoingPublishContext context)
        {
            context.SetDoNotEnforceBestPractices();
        }

        /// <summary>
        /// Turns off the best practice enforcement for the given context.
        /// </summary>
        public static void DoNotEnforceBestPractices(this UnsubscribeContext context)
        {
            context.SetDoNotEnforceBestPractices();
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