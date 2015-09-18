namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;

    class ApplyDefaultConsistencyGuaranteeBehavior:Behavior<RoutingContext>
    {
      
        public ApplyDefaultConsistencyGuaranteeBehavior(ConsistencyGuarantee transportDefault)
        {
            this.transportDefault = transportDefault;
        }

        public override Task Invoke(RoutingContext context, Func<Task> next)
        {
            ConsistencyGuarantee explicitGuarantee;

            if (!context.TryGet(out explicitGuarantee))
            {
                context.Set(transportDefault);
            }

            return next();
        }

        ConsistencyGuarantee transportDefault;

    }
}