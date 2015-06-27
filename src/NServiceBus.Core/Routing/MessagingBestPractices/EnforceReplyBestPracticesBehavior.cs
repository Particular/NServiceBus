namespace NServiceBus
{
    using System;
    using NServiceBus.MessagingBestPractices;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing.MessagingBestPractices;

    class EnforceReplyBestPracticesBehavior : Behavior<OutgoingReplyContext>
    {
        Validations validations;

        public EnforceReplyBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override void Invoke(OutgoingReplyContext context, Action next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForReply(context.GetMessageType());
            }

            next();
        }
    }
}