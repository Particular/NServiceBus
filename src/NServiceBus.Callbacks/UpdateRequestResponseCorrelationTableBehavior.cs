namespace NServiceBus
{
    using System;
    using NServiceBus.Callbacks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class UpdateRequestResponseCorrelationTableBehavior : PhysicalOutgoingContextStageBehavior
    {
        readonly RequestResponseStateLookup lookup;

        public UpdateRequestResponseCorrelationTableBehavior(RequestResponseStateLookup lookup)
        {
            this.lookup = lookup;
        }

        public override void Invoke(Context context, Action next)
        {
            RequestResponse.State state;

            if (context.Extensions.TryGet(out state))
            {
                lookup.RegisterState(context.MessageId, state);
            }
    
            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("UpdateRequestResponseCorrelationTable", typeof(UpdateRequestResponseCorrelationTableBehavior), "Updates the correlation table that keeps track of synchronous request/response callbacks")
            {
                InsertAfterIfExists(WellKnownStep.MutateOutgoingTransportMessage);
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }
}