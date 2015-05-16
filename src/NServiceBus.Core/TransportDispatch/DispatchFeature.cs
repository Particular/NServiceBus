namespace NServiceBus.Features
{
    class DispatchFeature:Feature
    {
        public DispatchFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {  
            context.Pipeline.RegisterConnector<DispatchMessageToTransportConnector>("Starts the message dispatch pipeline");
            context.Pipeline.RegisterConnector<DispatchTerminator>("Dispatches messages to the transport");
        }
    }
}