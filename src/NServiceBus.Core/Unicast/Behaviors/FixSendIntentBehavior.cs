namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    
    class FixSendIntentBehavior : IBehavior<OutgoingContext>
    {
        public void Invoke(OutgoingContext context, Action next)
        {
            if (context.OutgoingLogicalMessage.Headers.ContainsKey("$.temporary.ReplyToOriginator"))
            {
                context.OutgoingMessage.MessageIntent = MessageIntentEnum.Reply;
                context.OutgoingMessage.Headers.Remove("$.temporary.ReplyToOriginator");
                context.OutgoingLogicalMessage.Headers.Remove("$.temporary.ReplyToOriginator");
            }

            next();
        }
    }
}