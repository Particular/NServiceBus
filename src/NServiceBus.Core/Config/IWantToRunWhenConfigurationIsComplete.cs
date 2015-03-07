namespace NServiceBus.Config
{
    /// <summary>
    /// Implementors are invoked when configuration is complete.
    /// Implementors are resolved from the container so have access to full DI.
    /// </summary>
    [ObsoleteEx(
        Message = "Use the feature concept instead via A class which inherits from `NServiceBus.Features.Feature` and use `configuration.EnableFeature<YourClass>()`", 
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public interface IWantToRunWhenConfigurationIsComplete
    {
        /// <summary>
        /// Method invoked to run custom code.
        /// </summary>
        void Run(Configure config);
    }
}
