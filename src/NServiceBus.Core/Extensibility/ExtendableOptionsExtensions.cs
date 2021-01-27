namespace NServiceBus.Extensibility
{
    using Transport;

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
    }
}