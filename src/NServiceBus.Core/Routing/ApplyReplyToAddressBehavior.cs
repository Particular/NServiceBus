﻿namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;

    class ApplyReplyToAddressBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public ApplyReplyToAddressBehavior(string replyToAddress)
        {
            this.replyToAddress = replyToAddress;
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            context.Headers[Headers.ReplyToAddress] = replyToAddress;

            return next();
        }

        string replyToAddress;

    }
}