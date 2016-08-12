namespace NServiceBus
{
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Transport;

    class SatellitePipelineExecutor : IPipelineExecutor
    {
        public SatellitePipelineExecutor(IBuilder builder, SatelliteDefinition definition)
        {
            this.builder = builder;
            satelliteDefinition = definition;
        }

        public Task Invoke(MessageContext messageContext)
        {
            messageContext.Context.Set(messageContext.TransportTransaction);

            return satelliteDefinition.OnMessage(builder, messageContext);
        }

        SatelliteDefinition satelliteDefinition;
        IBuilder builder;
    }
}