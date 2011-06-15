using NServiceBus;

namespace SiteA
{
    using System;
    using log4net.Appender;
    using log4net.Core;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                    .Log4Net<ColoredConsoleAppender>(a => { a.Threshold = Level.Warn; })
                    .DefaultBuilder()
                    .BinarySerializer();
        }


    }

    internal class SetupGateway : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.Gateway();
        }
    }
}
