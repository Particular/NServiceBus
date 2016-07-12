namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transport;

    class SatellitePipelineExecutor : IPipelineExecutor
    {
        SatelliteDefinition satelliteDefinition;
        IBuilder builder;

        public SatellitePipelineExecutor(IBuilder builder, SatelliteDefinition definition)
        {
            this.builder = builder;
            satelliteDefinition = definition;
        }

        public Task Invoke(MessageContext messageContext)
        {
            return satelliteDefinition.OnMessage(builder, messageContext);
        }
    }
}