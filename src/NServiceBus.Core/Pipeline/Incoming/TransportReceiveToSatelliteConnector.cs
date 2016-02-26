namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class TransportReceiveToSatelliteConnector : StageConnector<ITransportReceiveContext, ISatelliteProcessingContext>
    {
        public override Task Invoke(ITransportReceiveContext context, Func<ISatelliteProcessingContext, Task> stage)
        {
            var physicalMessageContext = new SatelliteProcessingContext(context.Message, context);
            return stage(physicalMessageContext);
        }
    }
}