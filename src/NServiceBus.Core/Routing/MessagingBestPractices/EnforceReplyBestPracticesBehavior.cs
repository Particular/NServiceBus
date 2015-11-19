namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessagingBestPractices;
    using OutgoingPipeline;
    using Pipeline;
    using Routing.MessagingBestPractices;

    class EnforceReplyBestPracticesBehavior : Behavior<OutgoingReplyContext>
    {
        public EnforceReplyBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override Task Invoke(OutgoingReplyContext context, Func<Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.Extensions.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForReply(context.Message.MessageType);
            }

            return next();
        }

        Validations validations;
    }
}