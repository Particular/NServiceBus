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
        /// Get access to a dedicated "bucket" for the outgoing message to pass information down to the outgoing pipeline. In comparison with GetExtension method, settings set in this <see cref="ContextBag"/> are isolated for the given operation.
        /// </summary>        
        public static ContextBag GetMessageOperationExtensions(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);
            return options.MessageOperationContext;
        }

        /// <summary>
        /// Get access to the dedicated "bucket" for the outgoing message. In comparison with GetExtension method, settings set in this <see cref="ContextBag"/> are isolated for the given operation.
        /// </summary>
        public static ContextBag GetMessageOperationExtensions(this IBehaviorContext behaviorContext)
        {
            Guard.AgainstNull(nameof(behaviorContext), behaviorContext);
            if (!behaviorContext.Extensions.TryGet("MessageOperationContext", out ContextBag context))
            {
                context = new ContextBag();
                behaviorContext.Extensions.Set("MessageOperationContext", context);
            }

            return context;
        }
    }
}