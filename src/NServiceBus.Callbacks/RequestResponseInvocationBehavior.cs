namespace NServiceBus
{
    using System;
    using System.Linq;
    using NServiceBus.Callbacks;
    using NServiceBus.Features;
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

            object result;

            if (IsControlMessage(context.PhysicalMessage))
            {
                var taskCompletionSourceType = taskCompletionSource.GetType();
                var legacyEnumResponseType = taskCompletionSourceType.GenericTypeArguments[0];

                if (!CallbacksSupport.isLegacyEnumResponse(legacyEnumResponseType))
                {
                    var methodSetException = taskCompletionSource.GetType().GetMethod("SetException");
                    methodSetException.Invoke(taskCompletionSource, new object[]
                    {
                        new Exception(string.Format("Invalid response in control message. Expected '{0}' as the response type.", typeof(LegacyEnumResponse<>))),
                    });
                }

                var enumType = legacyEnumResponseType.GenericTypeArguments[0];
                var enumValue = transportMessage.Headers[Headers.ReturnMessageErrorCodeHeader];
                result = Activator.CreateInstance(legacyEnumResponseType, Enum.Parse(enumType, enumValue));
            }
            else
            {
                result = context.LogicalMessages.First().Instance;
            }

            var method = taskCompletionSource.GetType().GetMethod("SetResult");
            method.Invoke(taskCompletionSource, new[]
            {
                result
            });

            return true;
        }

        public static bool IsControlMessage(TransportMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader);
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RequestResponse_CallbackInvocation", typeof(RequestResponseInvocationBehavior), "Invokes the callback of a synchronous request/response")
            {
                InsertAfterIfExists("InvokeRegisteredCallbacks");
            }
        }
    }
}