namespace NServiceBus.Pipeline.Behaviors
{
    using System.Collections.Generic;
    using Unicast;
    using Unicast.Transport;

    public class CallbackInvocationBehavior : IBehavior
    {
        public const string CallbackInvokedKey = "NServiceBus.CallbackInvocationBehavior.CallbackWasInvoked";

        public IBehavior Next { get; set; }

        public IDictionary<string, BusAsyncResult> MessageIdToAsyncResultLookup { get; set; }

        public void Invoke(IBehaviorContext context)
        {
            var messageWasHandled = HandleCorrelatedMessage(context.TransportMessage, context.Messages);

            context.Set(CallbackInvokedKey, messageWasHandled);

            Next.Invoke(context);
        }

        bool HandleCorrelatedMessage(TransportMessage msg, object[] messages)
        {
            if (msg.CorrelationId == null)
                return false;

            if (msg.CorrelationId == msg.Id) //to make sure that we don't fire callbacks when doing send locals
                return false;

            BusAsyncResult busAsyncResult;

            lock (MessageIdToAsyncResultLookup)
            {
                MessageIdToAsyncResultLookup.TryGetValue(msg.CorrelationId, out busAsyncResult);
                MessageIdToAsyncResultLookup.Remove(msg.CorrelationId);
            }

            if (busAsyncResult == null)
                return false;

            var statusCode = int.MinValue;

            if (msg.IsControlMessage() && msg.Headers.ContainsKey(Headers.ReturnMessageErrorCodeHeader))
                statusCode = int.Parse(msg.Headers[Headers.ReturnMessageErrorCodeHeader]);

            busAsyncResult.Complete(statusCode, messages);

            return true;
        }
    }
}