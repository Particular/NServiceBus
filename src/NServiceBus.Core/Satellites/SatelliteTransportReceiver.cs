namespace NServiceBus.Satellites
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class SatelliteTransportReceiver : TransportReceiver
    {
        ISatellite satellite;


        public SatelliteTransportReceiver(string id, IBuilder builder, IDequeueMessages receiver, DequeueSettings dequeueSettings, PipelineBase<IncomingContext> pipeline, IExecutor executor, ISatellite satellite) 
            : base(id, builder, receiver, dequeueSettings, pipeline, executor)
        {
            this.satellite = satellite;
        }

        protected override void SetContext(IncomingContext context)
        {
            base.SetContext(context);
            context.Set(satellite);
        }

        public override void Start()
        {
            base.Start();
            satellite.Start();
        }

        public override void Stop()
        {
            base.Stop();
            satellite.Stop();
        }
    }
}