namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Linq;
    using Unicast;
    using Unicast.Transport;

    class CallbackInvocationBehavior : IBehavior<PhysicalMessageContext>
    {
        public const string CallbackInvokedKey = "NServiceBus.CallbackInvocationBehavior.CallbackWasInvoked";

        public UnicastBus UnicastBus { get; set; }

        public void Invoke(PhysicalMessageContext context, Action next)
        {
            var messageWasHandled = HandleCorrelatedMessage(context.PhysicalMessage, context.Get<LogicalMessages>());

            context.Set(CallbackInvokedKey, messageWasHandled);

            next();
        }

        bool HandleCorrelatedMessage(TransportMessage transportMessage, LogicalMessages messages)
        {
            if (transportMessage.CorrelationId == null)
            {
                return false;
            }

            if (transportMessage.CorrelationId == transportMessage.Id) //to make sure that we don't fire callbacks when doing send locals
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

            busAsyncResult.Complete(statusCode, messages.Select(lm=>lm.Instance).ToArray());

            return true;
        }
    }
}