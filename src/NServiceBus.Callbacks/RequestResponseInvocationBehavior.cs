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
        readonly RequestResponseStateLookup requestResponseStateLookup;

        public RequestResponseInvocationBehavior(RequestResponseStateLookup requestResponseStateLookup)
        {
            this.requestResponseStateLookup = requestResponseStateLookup;
        }

        public override void Invoke(Context context, Action next)
        {
            if (HandleCorrelatedMessage(context.PhysicalMessage, context))
            {
                context.MessageHandled = true;
            }

            next();
        }

        bool HandleCorrelatedMessage(TransportMessage transportMessage, Context context)
        {
            var correlationId = context.GetCorrelationId();

            if (correlationId == null)
            {
                return false;
            }

            string version;
            var checkMessageIntent = true;

            if (transportMessage.Headers.TryGetValue(Headers.NServiceBusVersion, out version))
            {
                if (version.StartsWith("3."))
                {
                    checkMessageIntent = false;
                }
            }

            if (checkMessageIntent && transportMessage.MessageIntent != MessageIntentEnum.Reply)
            {
                return false;
            }

            TaskCompletionSourceAdapter tcs;
            if (!requestResponseStateLookup.TryGet(correlationId, out tcs))
            {
                return false;
            }

            object result;

            if (IsControlMessage(context.PhysicalMessage))
            {
                var legacyEnumResponseType = tcs.ResponseType;

                if (!CallbackSupport.IsLegacyEnumResponse(legacyEnumResponseType))
                {
                    tcs.SetException(new Exception(string.Format("Invalid response in control message. Expected '{0}' as the response type.", typeof(LegacyEnumResponse<>))));
                }

                var enumType = legacyEnumResponseType.GenericTypeArguments[0];
                var enumValue = transportMessage.Headers[Headers.ReturnMessageErrorCodeHeader];
                result = Activator.CreateInstance(legacyEnumResponseType, Enum.Parse(enumType, enumValue));
            }
            else
            {
                result = context.LogicalMessages.First().Instance;
            }

            tcs.SetResult(result);

            return true;
        }

        static bool IsControlMessage(TransportMessage transportMessage)
        {
            return transportMessage.Headers != null &&
                   transportMessage.Headers.ContainsKey(Headers.ControlMessageHeader);
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RequestResponseInvocation", typeof(RequestResponseInvocationBehavior), "Invokes the callback of a synchronous request/response")
            {
            }
        }
    }
}