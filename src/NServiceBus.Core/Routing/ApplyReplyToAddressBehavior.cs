namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class ApplyReplyToAddressBehavior : Behavior<OutgoingContext>
    {

        public ApplyReplyToAddressBehavior(string replyToAddress)
        {
            this.replyToAddress = replyToAddress;
        }

        public override Task Invoke(OutgoingContext context, Func<Task> next)
        {
            context.SetHeader(Headers.ReplyToAddress, replyToAddress);

            return next();
        }

        string replyToAddress;

    }
}