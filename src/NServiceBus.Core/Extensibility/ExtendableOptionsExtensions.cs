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
        /// Get access to the dedicated "bucket" for the outgoing message. In comparison with GetExtension method, settings set in this <see cref="ContextBag"/> are isolated for the given operation.
        /// </summary>
        public static ContextBag GetOperationProperties(this IOutgoingContext behaviorContext) => GetOperationPropertiesInternal(behaviorContext);

        /// <summary>
        /// Get access to the dedicated "bucket" for the outgoing message. In comparison with GetExtension method, settings set in this <see cref="ContextBag"/> are isolated for the given operation.
        /// </summary>
        public static ContextBag GetOperationProperties(this IUnsubscribeContext behaviorContext) => GetOperationPropertiesInternal(behaviorContext);

        /// <summary>
        /// Get access to the dedicated "bucket" for the outgoing message. In comparison with GetExtension method, settings set in this <see cref="ContextBag"/> are isolated for the given operation.
        /// </summary>
        public static ContextBag GetOperationProperties(this ISubscribeContext behaviorContext) => GetOperationPropertiesInternal(behaviorContext);

        /// <summary>
        /// Get access to the dedicated "bucket" for the outgoing message. In comparison with GetExtension method, settings set in this <see cref="ContextBag"/> are isolated for the given operation.
        /// </summary>
        public static ContextBag GetOperationProperties(this IRoutingContext behaviorContext) => GetOperationPropertiesInternal(behaviorContext);

        static ContextBag GetOperationPropertiesInternal(this IBehaviorContext behaviorContext)
        {
            Guard.AgainstNull(nameof(behaviorContext), behaviorContext);
            if (!behaviorContext.Extensions.TryGet(MessageOperations.OperationPropertiesKey, out ContextBag context))
            {
                //context = new ContextBag();
                //behaviorContext.Extensions.Set(MessageOperations.OperationPropertiesKey, context);
                return behaviorContext.Extensions; // fallback behavior
            }

            return context;
        }
    }
}