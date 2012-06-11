namespace NServiceBus.Logging.Log4NetBridge
{
    public class ConfigureInternalLog4NetBridge : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            log4net.LogManager.ResetConfiguration();
            log4net.Config.BasicConfigurator.Configure(new Log4NetBridgeAppender());
        }
    }
}
