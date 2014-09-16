namespace NServiceBus.Config
{
    /// <summary>
    /// Implementors are invoked when configuration is complete.
    /// Implementors are resolved from the container so have access to full DI.
    /// </summary>
    public interface IWantToRunWhenConfigurationIsComplete
    {
        /// <summary>
        /// Method invoked to run custom code.
        /// </summary>
        void Run(Configure config);
    }
}
