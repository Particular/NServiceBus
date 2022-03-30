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
        /// <param name="options">Extendable options instance.</param>
        /// <returns>A big bucket.</returns>
        public static ContextBag GetExtensions(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);
            return options.Context;
        }

        /// <summary>
        /// TODO
        /// </summary>
        public static ContextBag GetMessageOperationExtensions(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);
            return options.MessageOperationContext;
        }

        internal static ContextBag GetMessageOperationExtensions(this IBehaviorContext behaviorContext)
        {
            if (!behaviorContext.Extensions.TryGet("MessageOperationContext", out ContextBag context))
            {
                context = new ContextBag();
                behaviorContext.Extensions.Set("MessageOperationContext", context);
            }

            return context;
        }
    }
}