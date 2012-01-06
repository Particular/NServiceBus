namespace NServiceBus.Hosting.Azure.LoggingHandlers
{
    /// <summary>
    /// Handles logging configuration for the production profile
    /// </summary>
    public class ProductionLoggingHandler : IConfigureLoggingForProfile<Production>
    {
        void IConfigureLogging.Configure(IConfigureThisEndpoint specifier)
        {
            // startup logging is handled outside nsb by wadcfg file
        }

       
    }
}