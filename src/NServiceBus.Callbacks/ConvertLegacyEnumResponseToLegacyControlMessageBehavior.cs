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
            if (CallbacksSupport.IsLegacyEnumResponse(context.MessageType))
            {
                context.Headers[Headers.ControlMessageHeader] = true.ToString();

                context.Headers.Remove(Headers.ContentType);
                context.Headers.Remove(Headers.EnclosedMessageTypes);

                context.Body = new byte[0];
            }

    
            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ConvertLegacyEnumResponseToLegacyControlMessageBehavior", typeof(ConvertLegacyEnumResponseToLegacyControlMessageBehavior), "Converts the legacy response message to a control message to support backward compatibiliy")
            {
                InsertAfterIfExists(WellKnownStep.MutateOutgoingTransportMessage);
                InsertBefore(WellKnownStep.DispatchMessageToTransport);
            }
        }
    }
}