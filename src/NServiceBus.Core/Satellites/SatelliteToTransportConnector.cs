namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;

    class SatelliteToTransportConnector : StageConnector<PhysicalMessageProcessingStageBehavior.Context, SatelliteContext>
    {
        public override void Invoke(PhysicalMessageProcessingStageBehavior.Context context, Action<SatelliteContext> next)
        {
            next(new SatelliteContext(context));
        }
    }
}