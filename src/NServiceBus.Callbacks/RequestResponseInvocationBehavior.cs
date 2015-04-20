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
            var invoked = HandleCorrelatedMessage(context.PhysicalMessage, context);

            context.Set("NServiceBus.CallbackInvocation.CallbackWasInvoked", invoked);

            next();
        }

        bool HandleCorrelatedMessage(TransportMessage transportMessage, Context context)
        {
            if (transportMessage.CorrelationId == null)
            {
                return false;
            }

            if (transportMessage.MessageIntent != MessageIntentEnum.Reply)
            {
                return false;
            }

            object taskCompletionSource;

            if (!requestResponseMessageLookup.TryGet(transportMessage.CorrelationId, out taskCompletionSource))
            {
                return false;
            }

            var method = taskCompletionSource.GetType().GetMethod("SetResult");
            method.Invoke(taskCompletionSource, new[]
            {
                context.LogicalMessages.First().Instance
            });

            return true;
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RequestResponse", typeof(RequestResponseInvocationBehavior), "Adds synchronous request/response messaging")
            {
                InsertAfterIfExists("InvokeRegisteredCallbacks");
            }
        }
    }
}