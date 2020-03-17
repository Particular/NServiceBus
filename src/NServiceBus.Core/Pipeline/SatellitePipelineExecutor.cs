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
            if (dispatcher == null)
            {
                dispatcher = builder.Build<IDispatchMessages>();
            }
            
            messageContext.Extensions.Set(messageContext.TransportTransaction);

            return satelliteDefinition.OnMessage(builder, dispatcher, messageContext);
        }

        SatelliteDefinition satelliteDefinition;
        IBuilder builder;
        IDispatchMessages dispatcher;
    }
}