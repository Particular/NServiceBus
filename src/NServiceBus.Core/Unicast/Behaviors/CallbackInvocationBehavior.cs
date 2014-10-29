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
            //All versions of 4 still mutated the intent back to send. So we can only apply this logic for version 5 and above
            if (SenderIsV5OrNewer(transportMessage))
            {
                if (transportMessage.MessageIntent != MessageIntentEnum.Reply)
                {
                    return false;
                }
            }
            else
            {
                //older versions used "Send" as intent for replies. Therefor we need to check for id != cid to avoid 
                // firing callbacks too soon
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

        bool SenderIsV5OrNewer(TransportMessage transportMessage)
        {
            string versionString;
            if (!transportMessage.Headers.TryGetValue(Headers.NServiceBusVersion, out versionString))
            {
                return false;
            }
            Version version;
            if (!Version.TryParse(versionString, out version))
            {
                // if we cant parse the version assume it is not V5
                return false;
            }
            return version.Major >= 5;
        }
    }
}