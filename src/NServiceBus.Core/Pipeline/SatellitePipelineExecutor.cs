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
            messageContext.Extensions.Set(messageContext.TransportTransaction);

            return !PipelineEventSource.Log.IsEnabled() ? satelliteDefinition.OnMessage(builder, messageContext) : InvokePipelineAndEmitEvents(messageContext);
        }

        async Task InvokePipelineAndEmitEvents(MessageContext messageContext)
        {
            var isFaulted = false;
            var pipelineEventSource = PipelineEventSource.Log;
            try
            {
                pipelineEventSource.SatelliteStart(satelliteDefinition.Name, messageContext.MessageId);
                await satelliteDefinition.OnMessage(builder, messageContext).ConfigureAwait(false);
            }
            catch
            {
                isFaulted = true;
                throw;
            }
            finally
            {
                pipelineEventSource.SatelliteStop(satelliteDefinition.Name, messageContext.MessageId, isFaulted);
            }
        }


        SatelliteDefinition satelliteDefinition;
        IBuilder builder;
    }
}