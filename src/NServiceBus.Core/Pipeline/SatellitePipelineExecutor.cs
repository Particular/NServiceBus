namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transport;

    class SatellitePipelineExecutor : IPipelineExecutor
    {
        public SatellitePipelineExecutor(IBuilder builder, SatelliteDefinition definition, IPipelineCache pipelineCache)
        {
            this.pipelineCache = pipelineCache;
            this.builder = builder;
            satelliteDefinition = definition;
        }

        public Task Invoke(MessageContext messageContext)
        {
            messageContext.Extensions.Set(builder);
            messageContext.Extensions.Set(messageContext.TransportTransaction);
            messageContext.Extensions.Set(pipelineCache);

            return satelliteDefinition.OnMessage(builder, messageContext);
        }

        SatelliteDefinition satelliteDefinition;
        IBuilder builder;
        IPipelineCache pipelineCache;
    }
}