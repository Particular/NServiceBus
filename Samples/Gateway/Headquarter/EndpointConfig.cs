using NServiceBus;

namespace Headquarter
{
    using log4net.Appender;
    using log4net.Core;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                    .Log4Net<ColoredConsoleAppender>(a =>{a.Threshold = Level.Warn;})
                    .DefaultBuilder()
                    .GatewayWithInMemoryPersistence();
        }
    }
}
