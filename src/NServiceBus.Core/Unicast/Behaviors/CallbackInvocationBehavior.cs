namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Messages;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Transport;

    class CallbackInvocationBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public const string CallbackInvokedKey = "NServiceBus.CallbackInvocationBehavior.CallbackWasInvoked";

        public UnicastBus UnicastBus { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {

            var messageWasHandled = HandleCorrelatedMessage(context.PhysicalMessage, context);

            context.Set(CallbackInvokedKey, messageWasHandled);

            next();
        }

        bool HandleCorrelatedMessage(TransportMessage transportMessage, ReceivePhysicalMessageContext context)
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

            IEnumerable<LogicalMessage> messages;

            if (!context.TryGet(out messages))
            {
                messages = new List<LogicalMessage>();
            }

            busAsyncResult.Complete(statusCode, messages.Select(lm=>lm.Instance).ToArray());

            return true;
        }
    }
}