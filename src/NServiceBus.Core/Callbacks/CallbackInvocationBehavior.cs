namespace NServiceBus.Callbacks
{
    using System;
    using System.Linq;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class CallbackInvocationBehavior : LogicalMessagesProcessingStageBehavior
    {
        readonly CallbackMessageLookup callbackMessageLookup;

        public CallbackInvocationBehavior(CallbackMessageLookup callbackMessageLookup)
        {
            this.callbackMessageLookup = callbackMessageLookup;
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

            if (!callbackMessageLookup.TryGet(transportMessage.CorrelationId, out taskCompletionSource))
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
                : base("RequestResponse", typeof(CallbackInvocationBehavior), "Adds request/response messaging")
            {
                InsertAfterIfExists(WellKnownStep.MutateIncomingTransportMessage);
                InsertBeforeIfExists(WellKnownStep.MutateIncomingMessages);
            }
        }
    }
}