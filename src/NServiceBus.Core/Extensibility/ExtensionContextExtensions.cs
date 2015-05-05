namespace NServiceBus.Extensibility
{
    /// <summary>
    /// Provides hidden access to the extension context
    /// </summary>
    public static class ExtensionContextExtensions
    {
        /// <summary>
        /// Gets access to a "bucket", this allows the developer to pass information from extension methods down to behaviors. 
        /// </summary>
        /// <param name="options">SendOptions instance.</param>
        /// <returns>A big bucket.</returns>
        public static ExtensionContext GetContext(this ExtendableOptions options)
        {
            return options.ExtensionContext;
        }

    }
}