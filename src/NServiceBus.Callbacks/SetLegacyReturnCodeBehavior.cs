namespace NServiceBus
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class SetLegacyReturnCodeBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            if (CallbackSupport.IsLegacyEnumResponse(context.MessageType))
            {
                context.Headers[Headers.ReturnMessageErrorCodeHeader] = ((dynamic)context.MessageInstance).ReturnCode;
            }
            next();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("SetLegacyReturnCode", typeof(SetLegacyReturnCodeBehavior), "Promotes the legacy return code to a header in order to be backwards compatible with v5 and below")
            {

            }
        }
    }
}