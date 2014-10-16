namespace NServiceBus
{
    using System;
    using System.Linq;
    using NServiceBus.Unicast.Transport;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

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

            if (SenderIsV4OrNewer(transportMessage))
            {
                if (transportMessage.MessageIntent != MessageIntentEnum.Reply)
                {
                    return false;
                }
            }
            else
            {
                //older versions used "Send" as intent for replies. Therefor we need to check for id != cid to avoid 
                // firing callbacks to soon
                if (transportMessage.Id == transportMessage.CorrelationId)
                {
                    return false;
                }
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

        bool SenderIsV4OrNewer(TransportMessage transportMessage)
        {
            string version;

            if (!transportMessage.Headers.TryGetValue(Headers.NServiceBusVersion, out version))
            {
                return false;
            }

            return !version.StartsWith("3");
        }
    }
}