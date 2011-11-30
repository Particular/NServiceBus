namespace NServiceBus
{
    using Config;
    using Gateway.Receiving;
    using Gateway.Sending;

    public class GatewayBootstrapper : IWantToRunWhenConfigurationIsComplete
    {
        public void Run()
        {
            //todo . introduce a IWantToRunWhenTheBusIsStarted
            Configure.Instance.Builder.Build<IStartableBus>()
                .Started += (s, e) =>
                                {
                                    if (!Configure.Instance.Configurer.HasComponent<GatewaySender>())
                                        return;

                                    Configure.Instance.Builder.Build<GatewaySender>().Start(GatewayConfiguration.GatewayInputAddress);
                                    Configure.Instance.Builder.Build<GatewayReceiver>().Start(GatewayConfiguration.GatewayInputAddress);
                                };
        }
    }
}