using NServiceBus;

namespace Headquarter
{
    using log4net.Appender;
    using log4net.Core;
    using NServiceBus.ObjectBuilder;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .Log4Net<ColoredConsoleAppender>(a => { a.Threshold = Level.Warn; })
                .DefaultBuilder()
                .Configurer.ConfigureComponent<NServiceBus.MasterNode.ConfigBacked.MasterNodeManager>(DependencyLifecycle.SingleInstance);
        }
    }

    internal class SetupGateway : IWantCustomInitialization
    {
        public void Init()
        {
            Configure.Instance.GatewayWithInMemoryPersistence();
        }
    }
}
