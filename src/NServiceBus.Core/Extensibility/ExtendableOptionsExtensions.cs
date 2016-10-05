namespace NServiceBus.Extensibility
{
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
            return options.Context;
        }
    }
}