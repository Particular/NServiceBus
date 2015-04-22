namespace NServiceBus
{
    using System;
    using NServiceBus.Callbacks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;

    class UpdateRequestResponseCorrelationTableBehavior : PhysicalOutgoingContextStageBehavior
    {
        readonly RequestResponseMessageLookup lookup;

        public UpdateRequestResponseCorrelationTableBehavior(RequestResponseMessageLookup lookup)
        {
            this.lookup = lookup;
        }

        public override void Invoke(Context context, Action next)
        {
            var sendOptions = context.DeliveryMessageOptions as SendMessageOptions;

            if (sendOptions == null)
            {
                next();
                return;
            }

            object tcs;
            if (!sendOptions.Context.TryGetValue("NServiceBus.RequestResponse.TCS", out tcs))
            {
                next();
                return;
            }

            lookup.RegisterResult(context.MessageId, tcs);

            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RequestResponse_update_correlation_table", typeof(UpdateRequestResponseCorrelationTableBehavior), "Updates the correlation table that keeps track of synchronous request/response callbacks")
            {
                InsertAfterIfExists(WellKnownStep.MutateOutgoingTransportMessage);
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }
}