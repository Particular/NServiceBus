namespace NServiceBus.Pipeline.Behaviors
{
    using System.Collections.Generic;
    using Unicast;
    using Unicast.Transport;

    class CallbackInvocationBehavior : IBehavior
    {
        public const string CallbackInvokedKey = "NServiceBus.CallbackInvocationBehavior.CallbackWasInvoked";

        public IBehavior Next { get; set; }

        public Dictionary<string, BusAsyncResult> MessageIdToAsyncResultLookup { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            var messageWasHandled = HandleCorrelatedMessage(context.TransportMessage, context.Messages);

            context.Set(CallbackInvokedKey, messageWasHandled);

            Next.Invoke(context);
        }

        bool HandleCorrelatedMessage(TransportMessage transportMessage, object[] messages)
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

            lock (MessageIdToAsyncResultLookup)
            {
                MessageIdToAsyncResultLookup.TryGetValue(transportMessage.CorrelationId, out busAsyncResult);
                MessageIdToAsyncResultLookup.Remove(transportMessage.CorrelationId);
            }

            if (busAsyncResult == null)
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

            busAsyncResult.Complete(statusCode, messages);

            return true;
        }
    }
}