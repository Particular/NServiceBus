namespace NServiceBus.Extensibility
{
    using Transport;
    using Pipeline;

    /// <summary>
    /// Provides hidden access to the extension context.
    /// </summary>
    public static class ExtendableOptionsExtensions
    {
        /// <summary>
        /// Gets access to a "bucket" that allows the developer to pass information from extension methods down to behaviors.
        /// </summary>        
        public static ContextBag GetExtensions(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);
            return options.Context;
        }

        /// <summary>
        /// Gets access to the <see cref="DispatchProperties"/> passed to the dispatcher.
        /// </summary>
        public static DispatchProperties GetDispatchProperties(this ExtendableOptions options)
        {
            Guard.AgainstNull(nameof(options), options);
            return options.DispatchProperties;
        }

        /// <summary>
        /// Get readonly access to the <see cref="ContextBag"/> used by the <see cref="ExtendableOptions"/> to capture any operation properties.
        /// </summary>
        public static IReadOnlyContextBag GetOperationProperties(this IOutgoingContext behaviorContext) => GetOperationPropertiesInternal(behaviorContext);

        /// <summary>
        /// Get readonly access to the <see cref="ContextBag"/> used by the <see cref="ExtendableOptions"/> to capture any operation properties.
        /// </summary>
        public static IReadOnlyContextBag GetOperationProperties(this IUnsubscribeContext behaviorContext) => GetOperationPropertiesInternal(behaviorContext);

        /// <summary>
        /// Get readonly access to the <see cref="ContextBag"/> used by the <see cref="ExtendableOptions"/> to capture any operation properties.
        /// </summary>
        public static IReadOnlyContextBag GetOperationProperties(this ISubscribeContext behaviorContext) => GetOperationPropertiesInternal(behaviorContext);

        /// <summary>
        /// Get readonly access to the <see cref="ContextBag"/> used by the <see cref="ExtendableOptions"/> to capture any operation properties.
        /// </summary>
        public static IReadOnlyContextBag GetOperationProperties(this IRoutingContext behaviorContext) => GetOperationPropertiesInternal(behaviorContext);

        static IReadOnlyContextBag GetOperationPropertiesInternal(this IBehaviorContext behaviorContext)
        {
            Guard.AgainstNull(nameof(behaviorContext), behaviorContext);
            if (!behaviorContext.Extensions.TryGet(ExtendableOptions.OperationPropertiesKey, out ContextBag context))
            {
                return behaviorContext.Extensions; // fallback behavior, e.g. when invoking the outgoing pipeline without using MessageOperation API.
            }

            return context;
        }
    }
}