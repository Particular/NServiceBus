namespace NServiceBus.Hosting.Azure.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the development profile
    /// </summary>
    public class DevelopmentLoggingHandler : IConfigureLoggingForProfile<Development>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            // startup logging is handled outside nsb by wadcfg file
        }
    }
}