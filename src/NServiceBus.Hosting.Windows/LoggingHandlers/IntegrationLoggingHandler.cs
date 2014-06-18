namespace NServiceBus.Hosting.Windows.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the integration profile.
    /// </summary>
    class IntegrationLoggingHandler : IConfigureLoggingForProfile<Integration>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
        }
    }
}