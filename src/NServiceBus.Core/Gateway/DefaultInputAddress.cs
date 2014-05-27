namespace NServiceBus.Gateway
{
    /// <summary>
    ///     Sets the default input address for the gateway
    /// </summary>
    class DefaultInputAddress : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            var gatewayInputAddress = Address.Parse(config.Settings.EndpointName()).SubScope("gateway");

            config.Settings.SetDefault("Gateway.InputAddress", gatewayInputAddress);
        }
    }
}