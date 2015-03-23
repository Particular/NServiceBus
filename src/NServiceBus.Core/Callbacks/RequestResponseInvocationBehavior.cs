namespace NServiceBus
{
    using System;
    using System.Linq;
    using NServiceBus.Callbacks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class RequestResponseInvocationBehavior : LogicalMessagesProcessingStageBehavior
    {
        readonly RequestResponseMessageLookup requestResponseMessageLookup;

        public RequestResponseInvocationBehavior(RequestResponseMessageLookup requestResponseMessageLookup)
        {
            this.requestResponseMessageLookup = requestResponseMessageLookup;
        }

        public override void Invoke(Context context, Action next)
        {
            HandleCorrelatedMessage(context.PhysicalMessage, context);

            next();
        }

        void HandleCorrelatedMessage(TransportMessage transportMessage, Context context)
        {
            if (transportMessage.CorrelationId == null)
            {
                return;
            }

            if (transportMessage.MessageIntent != MessageIntentEnum.Reply)
            {
                return;
            }

            object taskCompletionSource;

            if (!requestResponseMessageLookup.TryGet(transportMessage.CorrelationId, out taskCompletionSource))
            {
                return;
            }

            var method = taskCompletionSource.GetType().GetMethod("SetResult");
            method.Invoke(taskCompletionSource, new[]
            {
                context.LogicalMessages.First().Instance
            });
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RequestResponse", typeof(RequestResponseInvocationBehavior), "Adds request/response messaging")
            {
                InsertAfterIfExists(WellKnownStep.MutateIncomingTransportMessage);
                InsertBeforeIfExists(WellKnownStep.MutateIncomingMessages);
            }
        }
    }
}