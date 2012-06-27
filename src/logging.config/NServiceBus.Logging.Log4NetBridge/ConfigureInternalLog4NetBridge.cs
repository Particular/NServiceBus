namespace NServiceBus.Logging.Log4NetBridge
{
    public class ConfigureInternalLog4NetBridge : IWantToRunBeforeConfiguration
    {
        private static bool isConfigured;

        public void Init()
        {
            if (isConfigured)
                return;
            
            log4net.Config.BasicConfigurator.Configure(new Log4NetBridgeAppender());
            isConfigured = true;
        }
    }
}
