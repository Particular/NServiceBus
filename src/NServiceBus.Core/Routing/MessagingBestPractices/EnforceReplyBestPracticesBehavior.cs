namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using OutgoingPipeline;
    using Pipeline;

    class EnforceReplyBestPracticesBehavior : Behavior<OutgoingReplyContext>
    {
        public EnforceReplyBestPracticesBehavior(Validations validations)
        {
            this.validations = validations;
        }

        public override Task Invoke(OutgoingReplyContext context, Func<Task> next)
        {
            EnforceBestPracticesOptions options;

            if (!context.TryGet(out options) || options.Enabled)
            {
                validations.AssertIsValidForReply(context.Message.MessageType);
            }

            return next();
        }

        Validations validations;
    }
}