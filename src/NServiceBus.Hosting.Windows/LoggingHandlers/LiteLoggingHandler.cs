namespace NServiceBus.Hosting.Windows.LoggingHandlers
{

    /// <summary>
    /// Handles logging configuration for the lite profile.
    /// </summary>
    class LiteLoggingHandler : IConfigureLoggingForProfile<Lite>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
        }
    }
}