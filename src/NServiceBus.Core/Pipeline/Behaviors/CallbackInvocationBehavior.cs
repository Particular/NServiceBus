namespace NServiceBus.Pipeline.Behaviors
{
    using System.Collections.Concurrent;
    using Unicast;
    using Unicast.Transport;

    class CallbackInvocationBehavior : IBehavior
    {
        public const string CallbackInvokedKey = "NServiceBus.CallbackInvocationBehavior.CallbackWasInvoked";

        public IBehavior Next { get; set; }

        public ConcurrentDictionary<string, BusAsyncResult> MessageIdToAsyncResultLookup { get; set; }

        public void Invoke(BehaviorContext context)
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

            if (!MessageIdToAsyncResultLookup.TryRemove(transportMessage.CorrelationId, out busAsyncResult))
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