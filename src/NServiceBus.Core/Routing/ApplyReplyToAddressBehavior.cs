namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class ApplyReplyToAddressBehavior : Behavior<OutgoingContext>
    {

        public ApplyReplyToAddressBehavior(string replyToAddress)
        {
            this.replyToAddress = replyToAddress;
        }

        public override void Invoke(OutgoingContext context, Action next)
        {
            context.SetHeader(Headers.ReplyToAddress, replyToAddress);

            next();
        }

        string replyToAddress;

    }
}