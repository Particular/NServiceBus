namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transports;

    class SatellitePipelineInvoker : IPipelineInvoker
    {
        public SatellitePipelineInvoker(IBuilder builder, SatelliteDefinition definition)
        {
            this.builder = builder;
            this.definition = definition;
        }

        public Task Invoke(PushContext pushContext)
        {
            return definition.OnMessage(builder, pushContext);
        }

        SatelliteDefinition definition;
        IBuilder builder;
    }
}