namespace NServiceBus
{
    using Gateway.Receiving;
    using Gateway.Sending;
    using Unicast;

    public class GatewayBootstrapper : IWantToRunWhenTheBusStarts
    {
        public void Run()
        {
            if (!Configure.Instance.Configurer.HasComponent<GatewaySender>())
                return;
            
            Configure.Instance.Builder.Build<GatewaySender>().Start(ConfigureGateway.GatewayInputAddress);
            Configure.Instance.Builder.Build<GatewayReceiver>().Start(ConfigureGateway.GatewayInputAddress);
        }
    }
}