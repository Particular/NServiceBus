namespace NServiceBus
{
    using System;
    using NServiceBus.Callbacks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class UpdateRequestResponseCorrelationTableBehavior : PhysicalOutgoingContextStageBehavior
    {
        readonly RequestResponseMessageLookup lookup;

        public UpdateRequestResponseCorrelationTableBehavior(RequestResponseMessageLookup lookup)
        {
            this.lookup = lookup;
        }

        public override void Invoke(Context context, Action next)
        {
            ExtensionState state;

            if (context.Extensions.TryGet(out state))
            {
                lookup.RegisterResult(context.MessageId, state.TaskCompletionSource);
            }
    
            next();
        }

        public class ExtensionState
        {
            public ExtensionState(object taskCompletionSource)
            {
                TaskCompletionSource = taskCompletionSource;
            }

            public object TaskCompletionSource { get; private set; }
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