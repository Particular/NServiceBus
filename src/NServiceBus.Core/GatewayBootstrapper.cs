namespace NServiceBus
{
    using Gateway.Receiving;
    using Gateway.Sending;

    public class GatewayBootstrapper : IWantToRunWhenBusStartsAndStops
    {
        public void Start()
        {
            if (!Configure.Instance.Configurer.HasComponent<GatewaySender>())
                return;
            
            Configure.Instance.Builder.Build<GatewaySender>().Start(ConfigureGateway.GatewayInputAddress);
            Configure.Instance.Builder.Build<GatewayReceiver>().Start(ConfigureGateway.GatewayInputAddress);
        }

        public void Stop()
        {
            //TODO: Stop the gateway
        }
    }
}