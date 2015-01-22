namespace NServiceBus.Unicast.Transport
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Satellites;
    using NServiceBus.Transports;

    class SatelliteTransportReceiver : TransportReceiver
    {
        ISatellite satellite;


        public SatelliteTransportReceiver(string id, IBuilder builder, IDequeueMessages receiver, DequeueSettings dequeueSettings, PipelineExecutor pipelineExecutor, IExecutor executor, ISatellite satellite) 
            : base(id, builder, receiver, dequeueSettings, pipelineExecutor, executor)
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