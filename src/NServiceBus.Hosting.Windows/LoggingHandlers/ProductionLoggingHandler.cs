namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    using NServiceBus.Hosting.Profiles;

    /// <summary>
    /// Handles logging configuration for the <see cref="Production"/> profile.
    /// </summary>
    class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        public void Configure(IConfigureThisEndpoint specifier)
        {
            
        }
    }
}