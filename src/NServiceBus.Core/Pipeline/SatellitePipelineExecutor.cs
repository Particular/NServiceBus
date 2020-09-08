namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Transport;

    class SatellitePipelineExecutor : IPipelineExecutor
    {
        public SatellitePipelineExecutor(IServiceProvider builder, SatelliteDefinition definition)
        {
            this.builder = builder;
            satelliteDefinition = definition;
        }

        public Task Invoke(MessageContext messageContext, CancellationToken cancellationToken)
        {
            messageContext.Extensions.Set(messageContext.TransportTransaction);

            return satelliteDefinition.OnMessage(builder, messageContext);
        }

        SatelliteDefinition satelliteDefinition;
        IServiceProvider builder;
    }
}