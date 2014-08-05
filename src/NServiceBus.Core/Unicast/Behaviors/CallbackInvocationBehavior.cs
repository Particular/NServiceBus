namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Transport;

    class CallbackInvocationBehavior : IBehavior<IncomingContext>
    {
        public const string CallbackInvokedKey = "NServiceBus.CallbackInvocationBehavior.CallbackWasInvoked";

        public UnicastBus UnicastBus { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {

            var messageWasHandled = HandleCorrelatedMessage(context.PhysicalMessage, context);

            context.Set(CallbackInvokedKey, messageWasHandled);

            next();
        }

        bool HandleCorrelatedMessage(TransportMessage transportMessage, IncomingContext context)
        {
            if (transportMessage.CorrelationId == null)
            {
                return false;
            }

            if (transportMessage.MessageIntent != MessageIntentEnum.Reply)
            {
                return false;
            }

            BusAsyncResult busAsyncResult;

            if (!UnicastBus.messageIdToAsyncResultLookup.TryRemove(transportMessage.CorrelationId, out busAsyncResult))
            {
                return false;
            }

            var statusCode = int.MinValue;

            if (transportMessage.IsControlMessage())
            {
                string errorCode;
                if (transportMessage.Headers.TryGetValue(Headers.ReturnMessageErrorCodeHeader, out errorCode))
                {
                    statusCode = int.Parse(errorCode);
                }
            }

            busAsyncResult.Complete(statusCode, context.LogicalMessages.Select(lm => lm.Instance).ToArray());

            return true;
        }
    }
}