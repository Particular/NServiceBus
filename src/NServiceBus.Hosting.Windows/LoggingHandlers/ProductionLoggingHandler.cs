namespace NServiceBus.Hosting.Windows.LoggingHandlers
{

    /// <summary>
    /// Handles logging configuration for the production profile
    /// </summary>
    class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
        }

    }
}