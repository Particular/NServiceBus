namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class CaptureOutgoingLogicalMessageInstanceBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            context.Set("RequestResponse.OutgoingLogicalMessageInstance", context.OutgoingLogicalMessage.Instance);

            try
            {
                next();
            }
            finally
            {
                context.Remove("RequestResponse.OutgoingLogicalMessageInstance");
            }
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("RequestResponse_capture_outgoing_logical_message_instance", typeof(CaptureOutgoingLogicalMessageInstanceBehavior), "Captures the logical message being sent out, to be used later on by ConvertLegacyEnumResponseToLegacyControlMessageBehavior.")
            {
                
            }
        }
    }
}