namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class ConvertLegacyEnumResponseToLegacyControlMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var instance = context.Get<object>("RequestResponse.OutgoingLogicalMessageInstance");

            if (!CallbacksSupport.isLegacyEnumResponse(instance.GetType()))
            {
                next();
                return;
            }

            context.Headers[Headers.ReturnMessageErrorCodeHeader] = ((dynamic)instance).ReturnCode;
            context.Headers[Headers.ControlMessageHeader] = true.ToString();
            
            context.Headers.Remove(Headers.ContentType);
            context.Headers.Remove(Headers.EnclosedMessageTypes);
            
            context.Body = new byte[0];

            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RequestResponse_convert_legacy_enum_response_to_control_enum_message", typeof(ConvertLegacyEnumResponseToLegacyControlMessageBehavior), "Converts the legacy response message to a control message to support backward compatibiliy")
            {
                InsertAfterIfExists("LogOutgoingMessage");
                InsertAfterIfExists(WellKnownStep.MutateOutgoingTransportMessage);
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }
}