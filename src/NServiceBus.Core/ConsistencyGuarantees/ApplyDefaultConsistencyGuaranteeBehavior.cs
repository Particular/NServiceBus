namespace NServiceBus
{
    using System;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

    class ApplyDefaultConsistencyGuaranteeBehavior:Behavior<DispatchContext>
    {
      
        public ApplyDefaultConsistencyGuaranteeBehavior(ConsistencyGuarantee transportDefault)
        {
            this.transportDefault = transportDefault;
        }

        public override void Invoke(DispatchContext context, Action next)
        {
            ConsistencyGuarantee explicitGuarantee;

            if (!context.TryGet(out explicitGuarantee))
            {
                context.Set(transportDefault);
            }

            next();
        }

        readonly ConsistencyGuarantee transportDefault;

    }
}